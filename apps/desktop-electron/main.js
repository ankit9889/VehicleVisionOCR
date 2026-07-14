const { app, BrowserWindow, ipcMain } = require('electron');
const path = require('path');
const { spawn } = require('child_process');

let mainWindow;
let backendProcess;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 800,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      nodeIntegration: false,
      contextIsolation: true
    }
  });

  // In development, load the Vite dev server
  const startUrl = process.env.ELECTRON_START_URL || `file://${path.join(__dirname, '../frontend-react/dist/index.html')}`;
  mainWindow.loadURL(startUrl);

  if (process.env.ELECTRON_START_URL) {
    mainWindow.webContents.openDevTools();
  }

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

function startBackend() {
  const backendPath = path.join(__dirname, '../backend-dotnet/bin/Debug/net8.0/VehicleVisionOCR.Backend.exe');
  
  // In production, we'd start the compiled .exe
  // For development we can use dotnet run, but this is a simple setup
  if (process.env.NODE_ENV !== 'development') {
    backendProcess = spawn(backendPath, [], { detached: true });
    
    backendProcess.on('error', (err) => {
      console.error('Failed to start backend process:', err);
    });
  }
}

app.whenReady().then(() => {
  // startBackend(); // Start the ASP.NET Core process
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    if (backendProcess) {
      backendProcess.kill();
    }
    app.quit();
  }
});

app.on('quit', () => {
  if (backendProcess) {
    backendProcess.kill();
  }
});
