const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  // Expose any specific required IPC bindings here
  // For now, the frontend communicates securely with the local API on localhost:5000
  getAppVersion: () => process.env.npm_package_version
});
