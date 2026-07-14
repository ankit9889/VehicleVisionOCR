import React, { useEffect, useState } from 'react';
import { Box, Button, Card, CardContent, CardHeader, Divider, List, ListItem, ListItemText, Typography, CircularProgress, Chip } from '@mui/material';
import { useScannerStore } from '../stores/scannerStore';
import { apiClient } from '../api/client';
import { useSnackbar } from 'notistack';
import { LiveScannerDashboard } from '../components/LiveScannerDashboard';

export const Scanner: React.FC = () => {
  const { connectedScanner, status, setConnectedScanner, setStatus } = useScannerStore();
  const [availableScanners, setAvailableScanners] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const { enqueueSnackbar } = useSnackbar();

  const fetchScanners = async () => {
    try {
      setLoading(true);
      const res = await apiClient.get('/scanner/discover');
      setAvailableScanners(res.data);
    } catch (err) {
      enqueueSnackbar('Failed to fetch scanners', { variant: 'error' });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchScanners();
  }, []);

  const handleConnect = async (scannerId: string) => {
    try {
      setLoading(true);
      await apiClient.post(`/scanner/connect/${scannerId}`);
      setStatus('Connected');
      const scanner = availableScanners.find(s => s.info?.id === scannerId);
      if (scanner) setConnectedScanner(scanner.info);
      enqueueSnackbar('Scanner connected successfully', { variant: 'success' });
    } catch (err) {
      enqueueSnackbar('Failed to connect to scanner', { variant: 'error' });
    } finally {
      setLoading(false);
    }
  };

  const handleDisconnect = async () => {
    if (!connectedScanner) return;
    try {
      setLoading(true);
      await apiClient.post(`/scanner/disconnect/${connectedScanner.id}`);
      setStatus('Disconnected');
      setConnectedScanner(null);
      enqueueSnackbar('Scanner disconnected', { variant: 'info' });
    } catch (err) {
      enqueueSnackbar('Failed to disconnect', { variant: 'error' });
    } finally {
      setLoading(false);
    }
  };

  const handleTriggerScan = async () => {
    if (!connectedScanner) return;
    try {
      enqueueSnackbar('Triggering scanner...', { variant: 'info' });
      await apiClient.post(`/scanner/trigger/${connectedScanner.id}`);
      enqueueSnackbar('Scan triggered. Waiting for hardware...', { variant: 'success' });
    } catch (err) {
      enqueueSnackbar('Failed to trigger scan', { variant: 'error' });
    }
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" sx={{ fontWeight: 'bold' }}>Scanner Management</Typography>
        <Button variant="outlined" onClick={fetchScanners} disabled={loading}>
          Refresh Scanners
        </Button>
      </Box>

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3 }}>
        <Box sx={{ flex: '1 1 300px' }}>
          <Card elevation={2}>
            <CardHeader title="Available Devices" />
            <Divider />
            <List>
              {loading && availableScanners.length === 0 ? (
                <Box sx={{ p: 3, display: 'flex', justifyContent: 'center' }}><CircularProgress /></Box>
              ) : availableScanners.length === 0 ? (
                <ListItem><ListItemText primary="No scanners found" secondary="Ensure device is connected via USB" /></ListItem>
              ) : (
                availableScanners.map((s, idx) => (
                  <ListItem key={idx} divider>
                    <ListItemText primary={s.info?.name || 'Unknown Device'} secondary={`ID: ${s.info?.id}`} />
                    {connectedScanner?.id === s.info?.id ? (
                      <Chip label="Connected" color="success" size="small" />
                    ) : (
                      <Button size="small" variant="contained" onClick={() => handleConnect(s.info?.id)} disabled={loading || status === 'Connected'}>
                        Connect
                      </Button>
                    )}
                  </ListItem>
                ))
              )}
            </List>
          </Card>
        </Box>

        <Box sx={{ flex: '2 1 400px' }}>
          <Card elevation={2} sx={{ height: '100%' }}>
            <CardHeader title="Scanner Dashboard" />
            <Divider />
            <CardContent>
              {!connectedScanner ? (
                <Box sx={{ height: 200, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <Typography color="text.secondary">Select a scanner to view details</Typography>
                </Box>
              ) : (
                <Box>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
                    <Box sx={{ flex: 1 }}>
                      <Typography variant="subtitle2" color="text.secondary">Name</Typography>
                      <Typography variant="body1" sx={{ fontWeight: 'bold', mb: 2 }}>{connectedScanner.name}</Typography>
                      
                      <Typography variant="subtitle2" color="text.secondary">Serial Number</Typography>
                      <Typography variant="body1" sx={{ fontWeight: 'bold', mb: 2 }}>{connectedScanner.serialNumber || 'N/A'}</Typography>
                    </Box>
                    <Box sx={{ flex: 1 }}>
                      <Typography variant="subtitle2" color="text.secondary">Firmware</Typography>
                      <Typography variant="body1" sx={{ fontWeight: 'bold', mb: 2 }}>{connectedScanner.firmwareVersion || 'N/A'}</Typography>
                      
                      <Typography variant="subtitle2" color="text.secondary">Status</Typography>
                      <Chip label={status} color={status === 'Connected' ? 'success' : 'warning'} />
                    </Box>
                  </Box>

                  <Box sx={{ mt: 4, display: 'flex', gap: 2 }}>
                    <Button variant="contained" color="primary" size="large" onClick={handleTriggerScan}>
                      Trigger Capture
                    </Button>
                    <Button variant="outlined" color="error" size="large" onClick={handleDisconnect}>
                      Disconnect
                    </Button>
                  </Box>
                </Box>
              )}
            </CardContent>
          </Card>
        </Box>
      </Box>

      <Box sx={{ mt: 4 }}>
        <LiveScannerDashboard />
      </Box>
    </Box>
  );
};
