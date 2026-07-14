import React from 'react';
import { HashRouter, Routes, Route } from 'react-router-dom';
import { ThemeProvider } from '@mui/material/styles';
import { SnackbarProvider } from 'notistack';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

import { useSettingsStore } from './stores/settingsStore';
import { lightTheme, darkTheme } from './theme';

import { MainLayout } from './layout/MainLayout';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';
import { Scanner } from './pages/Scanner';
import { OCR } from './pages/OCR';
import { History } from './pages/History';
import { Logs } from './pages/Logs';
import { Settings } from './pages/Settings';
import { Colors } from './pages/Colors';
import { MobileScanner } from './pages/MobileScanner';

const queryClient = new QueryClient();

export const App: React.FC = () => {
  const { theme } = useSettingsStore();
  const currentTheme = theme === 'dark' ? darkTheme : lightTheme;

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={currentTheme}>
        <SnackbarProvider maxSnack={3} anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}>
          <HashRouter>
            <Routes>
              <Route path="/login" element={<Login />} />
              <Route path="/mobile-scanner" element={<MobileScanner />} />
              <Route path="/" element={<MainLayout />}>
                <Route index element={<Dashboard />} />
                <Route path="scanner" element={<Scanner />} />
                <Route path="ocr" element={<OCR />} />
                <Route path="history" element={<History />} />
                <Route path="logs" element={<Logs />} />
                <Route path="colors" element={<Colors />} />
                <Route path="settings" element={<Settings />} />
              </Route>
            </Routes>
          </HashRouter>
        </SnackbarProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
};

export default App;
