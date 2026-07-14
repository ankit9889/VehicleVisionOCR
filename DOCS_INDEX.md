# Application Documentation Index

The following documentation guides are necessary for deploying and maintaining the VehicleVisionOCR terminal.

## Administrator Guide
- Installer package can be deployed silently via MDM/SCCM using the standard NSIS `/S` switch.
- Application logs are generated daily in `%LocalAppData%\VehicleVisionOCR\Logs\`.
- Ensure target PCs have the **Zebra CoreScanner SDK for Windows** installed prior to deployment, as the COM plugins require the Zebra framework drivers.

## Scanner Setup Guide
1. Plug the Zebra scanner into the Windows PC via USB (or pair via Bluetooth to the cradle).
2. Install the Zebra 123Scan Utility (optional) or CoreScanner SDK.
3. Configure the scanner to run in **SNAPI** (Scanner Native API) mode. Keyboard wedge mode (HID) will *not* capture images.
4. Launch the VehicleVisionOCR application. The Dashboard will indicate "Connected".

## Troubleshooting Guide
- **Error:** Scanner Status stays "Disconnected".
  - **Resolution:** Verify scanner is in SNAPI mode. Verify CoreScanner service is running in Windows Services (`services.msc`).
- **Error:** UI displays "Failed to load history".
  - **Resolution:** The local backend failed to start or port 5000/5001 is bound by another application. Check the `Logs/backend-*.txt` for specific binding exceptions.

## Diagnostics & Support
Support ZIP generation is available via the Serilog rolling log files. 
Database backups (`vehicle_vision.db`) can be copied directly from the application root for manual offline restore procedures.
