# Vehicle Vision OCR - Project Overview

Welcome to the Obsidian Vault for the **VehicleVisionOCR** project.

## 🚀 Core Components
The system is divided into three main apps and a core scanner framework.

1. [[Backend Architecture]]: Built with ASP.NET Core. Handles OCR processing, background tasks, database persistence (SQLite), and scanner web APIs.
2. [[Frontend Architecture]]: Built with React (Vite) and TailwindCSS. Serves as the UI for the desktop app and the mobile web scanner.
3. [[Desktop Application]]: Electron wrapper to bundle the backend and frontend into a single standalone Windows executable installer.
4. [[Scanner System]]: A plugin-based hardware interface for Zebra barcode scanners and Mobile Web Cameras.
5. [[Troubleshooting]]: Common issues, API 500 errors, database resets, and network connectivity debugging.

## ⚙️ How It Works
- The Desktop App starts the Backend server (port 5256).
- The Desktop UI connects to the Backend API.
- The user can select a hardware scanner or the "Mobile Web Scanner".
- When "Mobile Web Scanner" is selected, the user accesses `http://<local-ip>:5173/mobile-scanner` on their phone to capture images.
- Images are POSTed to the backend, saved in SQLite, and queued for local OCR processing.
