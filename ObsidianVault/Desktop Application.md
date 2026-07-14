# Desktop Application

The Desktop wrapper is responsible for bundling the entire system into an easily installable `.exe` for Windows.

## 📁 Directory Structure
`apps/desktop-electron/`

## 🔧 Core Tech Stack
* **Framework**: Electron
* **Process Management**: `child_process` (spawns the .NET backend API as a detached process).
* **Packaging**: `electron-builder`

## 🚀 Lifecycle
1. **Startup**: Electron launches the `.NET 8` executable in the background. It waits for port `5256` to open.
2. **UI Load**: Once the backend is healthy, the Electron window loads the static React build (`apps/frontend-react/dist`).
3. **Shutdown**: When the Electron window is closed, it gracefully sends a kill signal to the backend process to prevent orphaned background tasks.

## 📦 Installer
The project will be built using `electron-builder`, producing a generic setup executable that automatically places the backend binaries and frontend assets in the correct local `AppData` directory to bypass Windows UAC restrictions.
