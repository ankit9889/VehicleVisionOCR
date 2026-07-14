import React, { useEffect, useState } from 'react';
import { Box, Card, CardContent, Typography, Button, Paper } from '@mui/material';
import { Refresh, Download } from '@mui/icons-material';
import { apiClient } from '../api/client';

export const Logs: React.FC = () => {
  const [logs, setLogs] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  const fetchLogs = async () => {
    try {
      setLoading(true);
      const res = await apiClient.get('/logs');
      setLogs(res.data);
    } catch (err) {
      console.error('Failed to fetch logs', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLogs();
  }, []);

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" sx={{ fontWeight: 'bold' }}>System Logs</Typography>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button variant="outlined" startIcon={<Refresh />} onClick={fetchLogs} disabled={loading}>
            Refresh
          </Button>
          <Button variant="contained" startIcon={<Download />}>
            Export Logs
          </Button>
        </Box>
      </Box>

      <Card elevation={2}>
        <CardContent sx={{ p: 0 }}>
          <Paper sx={{ p: 2, bgcolor: '#1e1e1e', color: '#00ff00', fontFamily: 'monospace', height: '600px', overflowY: 'auto', borderRadius: 0 }}>
            {logs.length === 0 && !loading ? (
              <Typography>No logs found...</Typography>
            ) : (
              logs.map((log, idx) => (
                <div key={idx} style={{ whiteSpace: 'pre-wrap', marginBottom: '4px' }}>
                  {log}
                </div>
              ))
            )}
            {loading && <Typography>Loading logs...</Typography>}
          </Paper>
        </CardContent>
      </Card>
    </Box>
  );
};
