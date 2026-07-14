# Frontend Architecture

The **VehicleVisionOCR** frontend is a modern SPA (Single Page Application) built with React.

## 📁 Directory Structure
`apps/frontend-react/`

## 🔧 Core Tech Stack
* **Framework**: React 18, Vite
* **Styling**: TailwindCSS
* **Routing**: React Router DOM
* **API Client**: Axios

## 🎨 UI/UX Features
- **Dashboard**: Provides a complete overview of today's scans, OCR queue status, and connection states.
- **Scanner Configuration**: Allows the admin to discover local scanners and connect/disconnect them.
- **Mobile Scanner Mode**: Hosted at `/mobile-scanner`. Provides an intuitive, full-screen camera interface utilizing the HTML5 `mediaDevices.getUserMedia` API. Captures image blobs and uploads them to the backend API over the local network.

## 📱 Network Connectivity
The frontend assumes it is hosted locally on the desktop (via Vite dev server or Electron production bundle) and accessed from mobile devices on the same Wi-Fi network using the host's IP address (e.g., `192.168.x.x:5173`). 

Axios intercepts network requests and defaults to the local backend port (5256). For mobile devices, it automatically resolves the hostname so it can connect to the backend running on the same host machine.
