# Backend Architecture

The backend of **VehicleVisionOCR** is a robust ASP.NET Core API application.

## 📁 Directory Structure
`apps/backend-dotnet/`

## 🔧 Core Tech Stack
* **Framework**: .NET 8.0 ASP.NET Core Web API
* **Database**: Entity Framework Core with SQLite provider (`VehicleVisionOCR.db`)
* **OCR**: Tesseract OCR (Local processing engine with tessdata)
* **Background Tasks**: `IHostedService` for async image processing queues

## 🚀 Key Services
- `ValidationEngine`: Regex-based detection for VINs and Barcodes (up to 25 chars). Handles locale-invariant string conversions.
- `ImageProcessingQueue`: A background worker that dequeues uploaded images and processes them using the Tesseract OCR engine without blocking the main thread.
- `TesseractOcrEngine`: Uses `PageSegMode.SparseText` to scan chaotic labels and extracts Color, Model, VIN, and Barcode (with dynamic fallback logic).
- `ScannerManager`: Manages the lifecycle and state of connected hardware and software scanners using the [[Scanner System]].

## 🌐 Endpoints
- `/api/mobilescanner/upload`: Accepts `multipart/form-data` image uploads from the mobile scanner app. It validates connection states before enqueueing the image.
- `/api/vehicles/*`: Handles data retrieval for the dashboard (stats, history).
- `/api/scanner/*`: Discovers, connects, and disconnects scanners.
