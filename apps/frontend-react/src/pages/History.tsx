import React, { useState } from 'react';
import { Box, Card, CardContent, Typography, TextField, MenuItem, Button, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper, Chip, Dialog, DialogTitle, DialogContent, DialogActions, Grid, Divider, Checkbox } from '@mui/material';
import { Search, Download, Delete } from '@mui/icons-material';
import { apiClient, getBackendBaseUrl } from '../api/client';

import { useSnackbar } from 'notistack';

export const History: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [history, setHistory] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedRow, setSelectedRow] = useState<any>(null);
  const [clearDialogOpen, setClearDialogOpen] = useState(false);
  const [bulkDeleteDialogOpen, setBulkDeleteDialogOpen] = useState(false);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const { enqueueSnackbar } = useSnackbar();

  const fetchHistory = async () => {
    try {
      const res = await apiClient.get('/vehicles/history');
      setHistory(res.data);
    } catch (err) {
      console.error('Failed to load history', err);
    } finally {
      setLoading(false);
    }
  };

  React.useEffect(() => {
    fetchHistory();
  }, []);

  const handleClearHistory = async () => {
    try {
      await apiClient.delete('/vehicles/clear-history');
      enqueueSnackbar('Scan history cleared successfully', { variant: 'success' });
      setHistory([]);
    } catch (err) {
      console.error('Failed to clear history', err);
      enqueueSnackbar('Failed to clear history', { variant: 'error' });
    } finally {
      setClearDialogOpen(false);
    }
  };

  const handleDeleteItem = async (id: string) => {
    try {
      await apiClient.delete(`/vehicles/history/${id}`);
      enqueueSnackbar('Scan record deleted', { variant: 'success' });
      setHistory(prev => prev.filter(row => row.id !== id));
      setSelectedRow(null);
    } catch (err) {
      console.error('Failed to delete history item', err);
      enqueueSnackbar('Failed to delete record', { variant: 'error' });
    }
  };

  const handleBulkDelete = async () => {
    try {
      await apiClient.post('/vehicles/history/bulk-delete', { ids: selectedIds });
      enqueueSnackbar(`${selectedIds.length} records deleted`, { variant: 'success' });
      setHistory(prev => prev.filter(row => !selectedIds.includes(row.id)));
      setSelectedIds([]);
    } catch (err) {
      console.error('Failed to bulk delete', err);
      enqueueSnackbar('Failed to delete selected records', { variant: 'error' });
    } finally {
      setBulkDeleteDialogOpen(false);
    }
  };

  const handleSelectAll = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.checked) {
      setSelectedIds(filteredHistory.map(row => row.id));
    } else {
      setSelectedIds([]);
    }
  };

  const handleSelectRow = (id: string) => {
    setSelectedIds(prev => 
      prev.includes(id) ? prev.filter(item => item !== id) : [...prev, id]
    );
  };

  const filteredHistory = history.filter(row => 
    row.plate.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <Box>
      <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 3 }}>Scan History</Typography>
      
      <Card elevation={2} sx={{ mb: 3 }}>
        <CardContent>
          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
            <TextField 
              label="Search Barcode" 
              variant="outlined" 
              size="small" 
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              sx={{ flexGrow: 1 }}
              slotProps={{ input: { startAdornment: <Search color="action" sx={{ mr: 1 }} /> } }}
            />
            <TextField select label="Status" size="small" sx={{ width: 150 }} defaultValue="All">
              <MenuItem value="All">All</MenuItem>
              <MenuItem value="Completed">Completed</MenuItem>
              <MenuItem value="Initiated">Initiated</MenuItem>
              <MenuItem value="Failed">Failed</MenuItem>
            </TextField>
            <Button variant="outlined" startIcon={<Download />}>Export CSV</Button>
            {selectedIds.length > 0 && (
              <Button 
                variant="contained" 
                color="error" 
                startIcon={<Delete />}
                onClick={() => setBulkDeleteDialogOpen(true)}
                sx={{ fontWeight: 'bold' }}
              >
                Delete Selected ({selectedIds.length})
              </Button>
            )}
            <Button 
              variant="outlined" 
              color="error" 
              onClick={() => setClearDialogOpen(true)}
              sx={{ fontWeight: 'bold' }}
            >
              Clear History
            </Button>
          </Box>
        </CardContent>
      </Card>

      <TableContainer component={Paper} elevation={2}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell padding="checkbox">
                <Checkbox 
                  indeterminate={selectedIds.length > 0 && selectedIds.length < filteredHistory.length}
                  checked={filteredHistory.length > 0 && selectedIds.length === filteredHistory.length}
                  onChange={handleSelectAll}
                />
              </TableCell>
              <TableCell>Date / Time</TableCell>
              <TableCell>Barcode</TableCell>
              <TableCell>Color</TableCell>
              <TableCell>Status</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <TableRow><TableCell colSpan={6} align="center">Loading...</TableCell></TableRow>
            ) : filteredHistory.length === 0 ? (
              <TableRow><TableCell colSpan={6} align="center">No records found</TableCell></TableRow>
            ) : filteredHistory.map((row) => (
              <TableRow key={row.id} hover selected={selectedIds.includes(row.id)}>
                <TableCell padding="checkbox">
                  <Checkbox 
                    checked={selectedIds.includes(row.id)} 
                    onChange={() => handleSelectRow(row.id)}
                  />
                </TableCell>
                <TableCell>{row.date}</TableCell>
                <TableCell>{row.plate}</TableCell>
                <TableCell>{row.color || 'N/A'}</TableCell>
                <TableCell>
                  <Chip label={row.status} color={row.status === 'Completed' ? 'success' : (row.status === 'Initiated' ? 'warning' : 'error')} size="small" />
                </TableCell>
                <TableCell align="right">
                  <Button size="small" onClick={() => setSelectedRow(row)}>View</Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog open={!!selectedRow} onClose={() => setSelectedRow(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Scan Details</DialogTitle>
        <DialogContent dividers>
          {selectedRow && (
            <Grid container spacing={2}>
              <Grid size={6}>
                <Typography variant="subtitle2" color="textSecondary">Date / Time</Typography>
                <Typography variant="body1" gutterBottom>{selectedRow.date}</Typography>
              </Grid>
              <Grid size={6}>
                <Typography variant="subtitle2" color="textSecondary">Status</Typography>
                <Chip label={selectedRow.status} color={selectedRow.status === 'Completed' ? 'success' : (selectedRow.status === 'Initiated' ? 'warning' : 'error')} size="small" />
              </Grid>
              <Grid size={6}>
                <Typography variant="subtitle2" color="textSecondary">Barcode</Typography>
                <Typography variant="body1" gutterBottom>{selectedRow.plate}</Typography>
              </Grid>
              
              {(selectedRow.make || selectedRow.model || selectedRow.year) && (
                <>
                  <Grid size={12}>
                    <Divider sx={{ my: 1 }} />
                  </Grid>
                  <Grid size={3}>
                    <Typography variant="subtitle2" color="textSecondary">Make</Typography>
                    <Typography variant="body1">{selectedRow.make || 'N/A'}</Typography>
                  </Grid>
                  <Grid size={3}>
                    <Typography variant="subtitle2" color="textSecondary">Model</Typography>
                    <Typography variant="body1">{selectedRow.model || 'N/A'}</Typography>
                  </Grid>
                  <Grid size={3}>
                    <Typography variant="subtitle2" color="textSecondary">Year</Typography>
                    <Typography variant="body1">{selectedRow.year || 'N/A'}</Typography>
                  </Grid>
                  <Grid size={3}>
                    <Typography variant="subtitle2" color="textSecondary">Color</Typography>
                    <Typography variant="body1">{selectedRow.color || 'N/A'}</Typography>
                  </Grid>
                </>
              )}

              {selectedRow.imageId && (
                <Grid size={12}>
                  <Divider sx={{ my: 1 }} />
                  <Typography variant="subtitle2" color="textSecondary" gutterBottom>Vehicle Image</Typography>
                  <Box sx={{ width: '100%', maxHeight: 300, display: 'flex', justifyContent: 'center', overflow: 'hidden', borderRadius: 1 }}>
                    <img 
                      src={`${getBackendBaseUrl()}/api/vehicles/image/${selectedRow.imageId}`}
                      alt="Vehicle Scan" 
                      style={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }} 
                    />
                  </Box>
                </Grid>
              )}

              <Grid size={12}>
                <Divider sx={{ my: 1 }} />
                <Typography variant="subtitle2" color="textSecondary" gutterBottom>Raw OCR Text</Typography>
                <Paper variant="outlined" sx={{ p: 2, bgcolor: 'background.default', maxHeight: 200, overflow: 'auto' }}>
                  <Typography variant="body2" component="pre" sx={{ m: 0, whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
                    {selectedRow.rawText || 'No text extracted.'}
                  </Typography>
                </Paper>
              </Grid>
            </Grid>
          )}
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setSelectedRow(null)}>Close</Button>
          {selectedRow && (
            <Button 
              onClick={() => handleDeleteItem(selectedRow.id)} 
              color="error" 
              variant="outlined"
            >
              Delete Record
            </Button>
          )}
        </DialogActions>
      </Dialog>

      <Dialog open={clearDialogOpen} onClose={() => setClearDialogOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle sx={{ fontWeight: 'bold', color: 'error.main' }}>Clear Scan History?</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete all scan history records? This action cannot be undone and will permanently delete all captured images and OCR logs.
          </Typography>
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setClearDialogOpen(false)} color="inherit" sx={{ fontWeight: 'bold' }}>Cancel</Button>
          <Button onClick={handleClearHistory} variant="contained" color="error" sx={{ fontWeight: 'bold' }}>Clear All History</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={bulkDeleteDialogOpen} onClose={() => setBulkDeleteDialogOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle sx={{ fontWeight: 'bold', color: 'error.main' }}>Delete Selected Records?</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete {selectedIds.length} selected record(s)? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setBulkDeleteDialogOpen(false)} color="inherit" sx={{ fontWeight: 'bold' }}>Cancel</Button>
          <Button onClick={handleBulkDelete} variant="contained" color="error" sx={{ fontWeight: 'bold' }}>Delete Selected</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
