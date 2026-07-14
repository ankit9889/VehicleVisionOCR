import React, { useEffect, useState } from 'react';
import { Typography, Box, Card, CardContent, Divider, LinearProgress } from '@mui/material';
import { DocumentScanner, CheckCircle, Timeline } from '@mui/icons-material';
import { useScannerStore } from '../stores/scannerStore';
import { apiClient } from '../api/client';

export const Dashboard: React.FC = () => {
  const { connectedScanner, status } = useScannerStore();
  const [stats, setStats] = useState({ todayScans: 0, pendingOcr: 0 });

  useEffect(() => {
    const fetchStats = async () => {
      try {
        const res = await apiClient.get('/vehicles/stats');
        setStats(res.data);
      } catch (err) {
        console.error('Failed to fetch stats', err);
      }
    };
    fetchStats();
    
    // Polling every 10 seconds
    const interval = setInterval(fetchStats, 10000);
    return () => clearInterval(interval);
  }, []);

  return (
    <Box>
      <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 3 }}>Dashboard</Typography>
      
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3 }}>
        {/* Scanner Status Card */}
        <Box sx={{ flex: '1 1 300px' }}>
          <Card elevation={2}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                <DocumentScanner color={connectedScanner ? 'primary' : 'disabled'} fontSize="large" />
                <Box>
                  <Typography variant="h6">Scanner Status</Typography>
                  <Typography variant="body2" color="text.secondary">
                    {connectedScanner ? connectedScanner.name : 'No Scanner Connected'}
                  </Typography>
                </Box>
              </Box>
              <Divider sx={{ my: 1 }} />
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2">Status</Typography>
                <Typography variant="body2" color={status === 'Connected' ? 'success.main' : 'error.main'} sx={{ fontWeight: 'bold' }}>
                  {status}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Box>

        {/* Scan Stats Card */}
        <Box sx={{ flex: '1 1 300px' }}>
          <Card elevation={2}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                <Timeline color="primary" fontSize="large" />
                <Box>
                  <Typography variant="h6">Today's Scans</Typography>
                  <Typography variant="h4" sx={{ fontWeight: 'bold' }}>{stats.todayScans}</Typography>
                </Box>
              </Box>
              <Divider sx={{ my: 1 }} />
              <Typography variant="body2" color="text.secondary">
                {stats.pendingOcr} pending OCR validation
              </Typography>
            </CardContent>
          </Card>
        </Box>

        {/* System Health Card */}
        <Box sx={{ flex: '1 1 300px' }}>
          <Card elevation={2}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                <CheckCircle color="success" fontSize="large" />
                <Box>
                  <Typography variant="h6">System Health</Typography>
                  <Typography variant="body2" color="text.secondary">Offline Terminal</Typography>
                </Box>
              </Box>
              <Divider sx={{ my: 1 }} />
              <Box sx={{ mt: 1 }}>
                <Typography variant="caption">CPU Usage (12%)</Typography>
                <LinearProgress variant="determinate" value={12} sx={{ mb: 1 }} />
                <Typography variant="caption">Storage (45%)</Typography>
                <LinearProgress variant="determinate" value={45} color="secondary" />
              </Box>
            </CardContent>
          </Card>
        </Box>
      </Box>
    </Box>
  );
};
