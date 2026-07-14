const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  // Add methods for communicating with main process if needed
  // E.g., for accessing file system, or triggering desktop-specific features
  onBackendReady: (callback) => ipcRenderer.on('backend-ready', callback)
});
