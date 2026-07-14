# Troubleshooting & Common Issues

This document outlines common errors that can occur during development or deployment of **VehicleVisionOCR** and how to fix them.

## 1. Dashboard API 500 Internal Server Errors
**Symptoms:** 
- The React frontend console shows `GET http://localhost:5256/api/vehicles/stats 500 (Internal Server Error)`.
- Backend logs show `SQLite Error 1: 'no such table: VehicleScans'`.

**Cause:**
The SQLite database (`vehicle_vision.db`) was created when the schema was incomplete. Entity Framework's `db.Database.EnsureCreated()` does not create missing tables if the `.db` file already exists. 

**Solution:**
For local development, simply delete the existing database file and restart the backend:
1. Stop the backend process.
2. Delete `vehicle_vision.db`, `vehicle_vision.db-shm`, and `vehicle_vision.db-wal` from the `apps/backend-dotnet` folder.
3. Run `dotnet run` again to generate a fresh database with the complete schema.

## 2. Electron Security Warnings
**Symptoms:**
- The console shows: `Electron Security Warning (Insecure Content-Security-Policy)`.

**Cause:**
This is an expected warning during development because the Vite server uses `unsafe-eval` for hot-module replacement (HMR). 

**Solution:**
This warning is safe to ignore during development and will automatically disappear when the app is packaged for production (`npm run build`).

## 3. Mobile Scanner Not Connecting
**Symptoms:**
- Error on mobile device: `Mobile scanner not connected to the desktop`.

**Cause:**
The backend isn't identifying the generic web camera as a valid scanner, or the frontend is hitting `localhost` instead of the local network IP.

**Solution:**
Ensure that your mobile device and desktop are on the same Wi-Fi network, and that you are accessing the frontend using the host machine's IP (e.g., `http://192.168.1.5:5173/mobile-scanner`). The `ScannerController` automatically accepts the connection.

## 4. OCR Fails to Extract Long Barcode Strings
**Symptoms:**
- The "Barcode" column in the Scan History displays `N/A`.
- Raw OCR text output completely misses the 15+ character alphanumeric string printed above the barcode.

**Cause:**
Standard Tesseract segmentation modes (like PSM 3) may ignore or misread widely spaced or very large characters, especially if they are close to vertical/horizontal barcode lines.

**Solution:**
The OCR engine is configured to use `PageSegMode.SparseText` (PSM 11), which treats the image as a sparse collection of text rather than structured paragraphs. Additionally, a dynamic fallback regex is implemented: if a 14-25 character barcode isn't found, the system defaults to the longest alphanumeric string on the label (ignoring known fields like "Model"), ensuring data capture is not lost.

## 5. React Console Warns About 'item' Prop
**Symptoms:**
- Console error: `Received 'true' for a non-boolean attribute 'item'`.

**Cause:**
MUI `Grid` versions handle props differently. Passing `item` to a `Grid` layout component that spreads props to the DOM causes this warning in newer versions.

**Solution:**
Remove the `item` prop from `<Grid>` elements and simply use sizing props directly (e.g., `<Grid xs={6}>`). This has been fixed in `History.tsx`.
