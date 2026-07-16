# OCR Engine Algorithms

The **VehicleVisionOCR** project features a highly advanced, multi-stage OCR pipeline designed to reliably extract data from non-standard vehicle labels (such as Colored labels with dense barcodes) using **Tesseract OCR** and **OpenCV (OpenCvSharp)**.

## Core Engines
The OCR functionality is split into two primary engines:
1. `TesseractOcrEngine` (Fallback / Full-Page)
2. `PositionBasedOcrEngine` (Structured / Standard Labels)

---

## 🎨 Color-Blind Preprocessing (MinRGB)
When dealing with colored labels (e.g., Red or Green text on a light background), standard grayscale conversions often cause the colored text to wash out or disappear completely. 

To solve this, we implemented the **MinRGB** algorithm:
Instead of `(R+G+B)/3`, the pipeline extracts the **minimum** value across the Red, Green, and Blue channels for every pixel:
```csharp
Cv2.Min(channels[0], channels[1], minBG);
Cv2.Min(minBG, channels[2], minRGB);
```
**Result**: Any colored ink (which naturally lacks high values in at least one channel) is rendered as pure, high-contrast black, while white backgrounds remain white. This guarantees that Tesseract receives a perfectly binarized image regardless of ink color.

---

## 📐 Dynamic Image Slicing (Position-Based Extraction)
For standard structured labels, passing the entire image to Tesseract often results in failure due to massive barcode density. The barcode visually overwhelms Tesseract's `PageSegMode.Auto` causing it to hallucinate text (e.g., extracting `11111111` and completely ignoring the actual text).

The `PositionBasedOcrEngine` crops the image into specific fields (VIN, Model, Color) *before* passing them to Tesseract.

### 1. Dynamic Barcode Avoidance (Horizontal Projection Profile)
Because label alignment and background white space fluctuate, hardcoded crop percentages (e.g., Top 45%) can accidentally slice the VIN in half or include barcode lines.
To find the **exact** vertical coordinates of the barcode:
1. **Morphological Close**: The image is thresholded and a `50x2` horizontal kernel connects barcode lines into a solid block, without merging vertically with the VIN.
2. **Row Pixel Sums (Projection)**: OpenCV's `Cv2.Reduce` creates a 1D column matrix summarizing the white pixels in each row.
3. **Block Detection**: The algorithm finds the **tallest continuous block** of rows where the ink density exceeds 30%. This is mathematically guaranteed to be the barcode.
4. **Cropping**: The image is perfectly sliced 5 pixels above the barcode (for the VIN crop) and 5 pixels below the barcode (for the Model/Color bottom crop).

### 2. Dynamic Column Splitting (Vertical Projection Profile)
The bottom crop contains the Model and Color fields side-by-side:
`CB190XS ID           K1LJ D10 ID`
`PB396                GRANITE BLACK MICA`

A hardcoded 50% horizontal split often slices through words like "GRANITE".
To find the exact split point:
1. **Vertical Projection**: `Cv2.Reduce` sums the pixels in each column.
2. **Gap Detection**: The algorithm scans the middle 50% of the image to find the widest continuous gap (columns with <= 3 noise pixels).
3. **Splitting**: The bottom image is sliced exactly down the center of this dynamic empty gap, ensuring no words are ever cut in half.

---

## 🛡️ Smart Regex Filtering
Rather than relying on image-destroying morphological erasers to remove barcode noise (which often erased thin valid characters like the 'I' in 'ID'), the pipeline uses **C# Regex Line Filtering**.

If Tesseract accidentally reads barcode edges as `l I l I l`, the post-processor filters out the garbage by ensuring extracted lines contain a minimum alphanumeric complexity (e.g., `l.Distinct().Count() >= 4` and valid regex matches), completely isolating the correct Model and Color strings.

---

## 🔠 Optical Character Confusion (The "5" vs "S" Problem)
Due to dense and sometimes overlapping kerning in standard vehicle manufacturing labels, Tesseract consistently misidentifies certain characters based on surrounding patterns.

**Example Conflict:** The letters `S` and `5`, and `3` and `S` are often visually identical when printed closely together. A VIN like `A2S3D` may be incorrectly scanned as `A23D` (merging the S and 3), or `NE5LD5` as `NESLDS`.

### 1. Global VIS Position Rules
For standard 17-character VINs, the VIS (Vehicle Indicator Section - last 6 characters) must be numerical.
The `TesseractOcrEngine` employs a fixed character-position rule:
* If the extracted text is exactly 17 characters, it loops through the **last 6 characters** and hard-replaces confused letters (`S` -> `5`, `B` -> `8`, `Z` -> `2`, `P` -> `0`).
* It globally replaces illegal VIN letters (`I`, `O`, `Q`) everywhere in the string.

### 2. Prefix-Based Dictionary Corrections
If a VIN is *not* exactly 17 characters long (e.g., due to extra manufacturer characters printed alongside it like `A2S3D...00`), the global VIS position logic is safely bypassed to avoid corrupting actual text.
To handle kerning issues in the WMI/VDS prefix sections (first 8 characters), the engine uses a highly specific Prefix Dictionary in `appsettings.json`:
* `NESLDS` -> `NE5LD5`
* `A23D` -> `A2S3D`

This specific block-replacement guarantees that manufacturers' font quirks are corrected without relying on dangerous global search-and-replaces across the entire string.

---

## 🧠 Intelligent OCR Correction Pipeline (Strategy Pattern)
Replacing the older global search-and-replace mechanics, the engine now uses a modular, SOLID-compliant OCR Correction Pipeline (`OcrCorrectionCoordinator`).

### 1. The Strategy Coordinator
The `OcrCorrectionCoordinator` dynamically routes raw OCR extractions (VIN, Color, etc.) to specific `IOcrCorrectionStrategy` instances. This prevents cross-contamination of logic (e.g., Color rules affecting VINs).

### 2. The VIN Pipeline (ISO 3779)
The VIN strategy implements multiple discrete passes:
*   **Normalizer**: Cleans hyphens and applies universally safe mappings (O->0, I->1) and positional mappings (VIS characters must be numeric). It also identifies and corrects misread Check Digits (e.g., OCR 'G' -> '6').
*   **Candidate Generator**: If the scanned prefix has a minor typo (e.g., `LBB` instead of `LB8`), it queries an `IWmiRepository` using an optimized memory-cached `SemaphoreSlim` lock and generates valid candidates using fuzzy Levenshtein matching.
*   **Scoring Service**: Ranks candidates based on Tesseract Confidence (40%), Regex pattern integrity (30%), and the ISO 3779 Mathematical Check Digit Validation (30%). To prevent false negatives for international VINs (like `ME4` India or `S` Europe), strict Modulus 11 validation is bypassed for regions where it is not legally mandated (non-NA and non-China). A candidate must pass the `MinVinScoreThreshold` to be accepted.

### 3. The Color Pipeline
Uses an `IColorRepository` to compare the raw text against known active vehicle colors.
*   **Performance Levenshtein**: Uses an `ArrayPool<int>` backed 1-dimensional array for Levenshtein calculations to completely eliminate GC allocations (preventing cache/heap exhaustion when iterating through database colors).
*   **Matching Modes**: Checks for exact matches, token-based partial matches (e.g. `LUNAR METALLIC` -> `LUNAR SILVER METALLIC`), and standard fuzzy matches.

### 4. Explainable Domain Models
Every correction results in a rich `CorrectionResult` containing `OriginalText`, `CorrectedText`, `ConfidenceLevel`, and `AppliedRules`. If the engine is unsure, it gracefully returns the raw value unchanged (`Passthrough` mode) instead of hallucinating data..
