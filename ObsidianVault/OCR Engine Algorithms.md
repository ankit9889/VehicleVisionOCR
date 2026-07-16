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
