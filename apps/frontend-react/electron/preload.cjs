const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  // Expose IPC endpoints here if needed
  getVersion: () => process.env.npm_package_version || '1.0.0'
});
