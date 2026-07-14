# Scanner System Framework

The **Scanner System** is a modular, interface-driven plugin architecture that allows VehicleVisionOCR to communicate with both physical hardware scanners and the virtual mobile web scanner.

## 📁 Location
`src/VehicleVisionOCR.Scanner.Core/` (Interfaces and Manager)
`src/VehicleVisionOCR.Scanner.Zebra/` (Hardware Plugin)
`src/VehicleVisionOCR.Scanner.MobileWeb/` (Mobile Web Plugin)

## 🔌 Architecture
- `IScanner`: The base interface for any scanner. Exposes properties like `State` (Connected, Ready, Error) and `ScannerInfo`.
- `IScannerManager`: Handles dependency injection, discovery of plugins, and routing events.
- `ScannerBrand`: Enum representing the hardware brand (Zebra, Generic, etc.).

## 📱 Mobile Web Scanner
The `MobileWeb` plugin mimics a physical scanner. Instead of listening to a USB COM port, it registers itself as `ScannerBrand.Generic`. When the frontend uploads an image to the API, the `MobileScannerController` passes the image bytes to this plugin via `ReceiveImage()`, which fires the `OnImageCaptured` event exactly like a hardware scanner would.

## 🦓 Zebra CoreScanner
The `Zebra` plugin uses the Motorola CoreScanner API (COM interop) to read barcode data from physical USB scanners connected to the Windows host machine.
