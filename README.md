# VehicleVisionOCR

VehicleVisionOCR is a highly decoupled, offline-first enterprise desktop application designed for high-speed hardware scanning and intelligent Vehicle Identification Number (VIN) and License Plate extraction via Optical Character Recognition (OCR).

## Features
- **Offline First:** Zero internet connectivity required. Fully air-gapped system.
- **Hardware Integration:** Native COM-level integration with Zebra CoreScanner hardware.
- **Background Pipeline:** Asynchronous `System.Threading.Channels` queueing prevents UI locking during intensive OCR tasks.
- **Enterprise SQLite:** Configured with WAL mode for high-concurrency read/writes.
- **Unified Installer:** Packaged as a single NSIS executable containing both the Electron/React UI and the self-contained .NET 8 backend.

## Deployment Instructions
1. Install `Zebra CoreScanner SDK` on the target machine.
2. Run the `VehicleVisionOCR Setup 1.0.0.exe` installer generated from `npm run build`.
3. Launch the application from the desktop shortcut.

## Architecture
Built using Clean Architecture principles, the application separates Domain, Application, and Infrastructure concerns. Hardware interaction (Zebra) and OCR execution (Tesseract) are strictly isolated via a Plugin Architecture.

For more details, see the generated `FINAL_REPORT.md` and `RELEASE_CHECKLIST.md` documents.
