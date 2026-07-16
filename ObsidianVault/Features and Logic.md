# Features and Logic

**VehicleVisionOCR** is designed as a hybrid hardware-software solution for vehicle manufacturing and logistics tracking. Below is a comprehensive breakdown of the core features and the underlying logic powering them.

---

## 1. Multi-Source Image Acquisition
The system supports capturing images from entirely different hardware ecosystems simultaneously.

### 📱 Mobile Web Scanner
* **Feature**: Allows workers to use their smartphone cameras to scan labels without installing any native app.
* **Logic**: The React frontend serves a `/mobile-scanner` route. When a worker connects to the local IP of the server via their phone's browser, the browser's `navigator.mediaDevices.getUserMedia` API accesses the camera. Images are captured as Base64/Blobs and sent via `multipart/form-data` POST requests to the local C# backend.
* **Benefit**: Zero-install deployment on the warehouse floor.

### 🔫 Zebra Hardware Integration
* **Feature**: Native support for industrial Zebra barcode scanners via USB (SNAPI mode).
* **Logic**: The `ZebraScannerProvider` interfaces with the CoreScanner API (COM Object). It registers for `BarcodeEvent` (for fast 1D/2D scanning) and `ImageEvent` (to pull raw high-res images from the scanner's internal camera). 
* **Benefit**: High durability and extremely fast trigger-based hardware scanning.

---

## 2. Advanced Preprocessing & Deskewing
Before OCR can read text, the image must be perfectly straight and high-contrast.

### 📐 Auto-Deskew Logic
* **Feature**: Automatically rotates tilted or skewed labels to be perfectly horizontal.
* **Logic**: The `ImageDeskew` class uses OpenCV. It converts the image to grayscale, applies Otsu's binarization, and finds the contours of the text blocks. By calculating the minimum area bounding rectangle (`MinAreaRect`) around the text, it determines the angle of tilt. An Affine Transformation matrix is then calculated and applied to rotate the entire image back to 0 degrees.

### 🎨 Color-Blind (MinRGB) Binarization
* **Feature**: Extracts text seamlessly regardless of whether the ink is black, red, green, or blue.
* **Logic**: Instead of a standard grayscale average `(R+G+B)/3` which washes out colors, the algorithm computes `Min(R, Min(G, B))`. Since colored inks are missing high values in at least one RGB channel, the minimum channel is always dark. This forces colored text to become pitch black while the white background remains bright white.

---

## 3. Dual-Strategy OCR Pipeline
The system intelligently decides how to extract data based on the label type.

### 🏗️ Position-Based Extraction (Standard Labels)
* **Feature**: Used for consistent, structured labels where fields are always in the same relative area.
* **Logic**: Uses **Horizontal & Vertical Projection Profiles**. By counting the number of white pixels in rows and columns, the engine dynamically finds the empty gaps (whitespace) between text blocks. It uses these gaps to crop the image into perfect isolated squares (VIN box, Model box, Color box) while entirely avoiding dense Barcode regions. These clean squares are then passed to Tesseract configured for `SingleBlock` reading, achieving near 100% accuracy.

### 🧠 Adaptive Full-Page Extraction (Non-Standard Labels)
* **Feature**: Used for damaged, chaotic, or completely random label formats.
* **Logic**: The image goes through 9 different computer vision passes (CLAHE, Adaptive Thresholding, Blur, Erode, MinRGB, etc.). Tesseract reads every pass using `SparseText` mode. The engine then uses Regex (`[A-HJ-NPR-Z0-9]{17}`) to hunt for VIN patterns across all 9 raw text outputs. The result with the highest Tesseract confidence score is selected.

---

## 4. Smart Validation & Error Correction
OCR is never 100% perfect, so the system employs logic to fix common Tesseract hallucinations.

### 🛡️ VIN Sanitization
* **Feature**: Prevents illegal characters in Vehicle Identification Numbers.
* **Logic**: By international standard, VINs never contain the letters `I`, `O`, or `Q`. The `ApplyOcrCorrections` function automatically forces `O -> 0` and `I -> 1`. Furthermore, for the last 6 characters (which must be numerical serial numbers), it forces common letter confusions back to numbers (`S -> 5`, `B -> 8`, `Z -> 2`).

### 🎨 Color Dictionary Matching
* **Feature**: Detects if a scanned color is real or misspelled.
* **Logic**: The backend contains a known whitelist database of manufacturer colors (e.g., "GRANITE BLACK MICA"). When the OCR extracts a color, it calculates the **Levenshtein Distance** against the database to catch small spelling mistakes. If the color doesn't match perfectly, it flags a warning to the admin dashboard.

---

## 5. Offline Queue & Admin Dashboard
* **Feature**: Completely offline processing with a human-in-the-loop review system.
* **Logic**: When an image is received, it is saved to a local SQLite database with a `Pending` status. An `IHostedService` background queue picks it up, runs the heavy OCR algorithms on a background thread so the API never blocks, and updates the database. The React frontend polls this database and presents a "Review" UI where an admin can visually see the original crop and manually fix the Extracted Data before hitting "Approve & Save".
