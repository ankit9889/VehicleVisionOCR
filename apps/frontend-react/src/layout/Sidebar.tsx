import React from 'react';
import { Box, Drawer, List, ListItem, ListItemButton, ListItemIcon, ListItemText, Typography, Divider } from '@mui/material';
import { Dashboard, DocumentScanner, History, Settings, ReceiptLong, Code, ColorLens } from '@mui/icons-material';
import { useNavigate, useLocation } from 'react-router-dom';

const DRAWER_WIDTH = 260;

export const Sidebar: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();

  const menuItems = [
    { text: 'Dashboard', icon: <Dashboard />, path: '/' },
    { text: 'Scanner', icon: <DocumentScanner />, path: '/scanner' },
    { text: 'OCR processing', icon: <ReceiptLong />, path: '/ocr' },
    { text: 'Scan History', icon: <History />, path: '/history' },
    { text: 'System Logs', icon: <Code />, path: '/logs' },
    { text: 'Colors', icon: <ColorLens />, path: '/colors' },
    { text: 'Settings', icon: <Settings />, path: '/settings' },
  ];

  return (
    <Drawer
      variant="permanent"
      sx={{
        width: DRAWER_WIDTH,
        flexShrink: 0,
        '& .MuiDrawer-paper': { width: DRAWER_WIDTH, boxSizing: 'border-box' },
      }}
    >
      <Box sx={{ p: 3 }}>
        <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
          VehicleVision
        </Typography>
        <Typography variant="caption" color="text.secondary">
          OCR Offline Terminal
        </Typography>
      </Box>
      <Divider />
      <List sx={{ px: 2 }}>
        {menuItems.map((item) => (
          <ListItem key={item.text} disablePadding sx={{ mb: 1 }}>
            <ListItemButton
              selected={location.pathname === item.path}
              onClick={() => navigate(item.path)}
              sx={{ borderRadius: 2 }}
            >
              <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
              <ListItemText primary={item.text} sx={{ '& .MuiTypography-root': { fontWeight: 500 } }} />
            </ListItemButton>
          </ListItem>
        ))}
      </List>
    </Drawer>
  );
};
