import axios from 'axios';

// Dynamically determine the backend URL.
// If running inside Electron (file://), connect to localhost:5256
// If accessed via Mobile browser, connect to the PC's local IP address on port 5256.
const isFileProtocol = window.location.protocol === 'file:';
export const getBackendBaseUrl = () => isFileProtocol 
  ? 'http://localhost:5256' 
  : `${window.location.protocol}//${window.location.hostname}:5256`;

// Configure the base URL for the local ASP.NET Core backend
export const apiClient = axios.create({
  baseURL: `${getBackendBaseUrl()}/api`,
  withCredentials: true, // Crucial for cookie-based auth
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    // Global error handling, particularly for unauthorized access
    if (error.response && error.response.status === 401) {
      // Trigger a custom event or manipulate the store directly
      window.dispatchEvent(new CustomEvent('auth-error'));
    }

    const config = error.config;
    if (config) {
      config.retryCount = config.retryCount || 0;
      
      // Auto-retry for ERR_NETWORK on startup (backend takes a few seconds to start)
      if (!error.response && config.retryCount < 5) {
        config.retryCount += 1;
        // Wait 1 second before retrying
        await new Promise(resolve => setTimeout(resolve, 1000));
        return apiClient(config);
      }
    }

    return Promise.reject(error);
  }
);
