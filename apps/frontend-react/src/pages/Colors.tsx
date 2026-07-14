import React, { useEffect, useState } from 'react';
import { Box, Card, CardContent, Typography, TextField, Button, Chip, CircularProgress } from '@mui/material';
import { useSnackbar } from 'notistack';
import { apiClient } from '../api/client';

export const Colors: React.FC = () => {
  const [availableColors, setAvailableColors] = useState<string[]>([]);
  const [newColorInput, setNewColorInput] = useState('');
  const [loading, setLoading] = useState(true);
  const { enqueueSnackbar } = useSnackbar();

  useEffect(() => {
    apiClient.get('/colors')
      .then(res => {
        setAvailableColors(res.data);
        setLoading(false);
      })
      .catch(err => {
        console.error(err);
        enqueueSnackbar('Failed to load colors', { variant: 'error' });
        setLoading(false);
      });
  }, [enqueueSnackbar]);

  const handleAddColor = async () => {
    if (!newColorInput.trim()) return;
    try {
      const res = await apiClient.post('/colors', { name: newColorInput });
      if (res.data.success) {
        setAvailableColors(prev => [...prev, res.data.color].sort());
        setNewColorInput('');
        enqueueSnackbar('Color added successfully!', { variant: 'success' });
      }
    } catch (err: any) {
      if (err.response?.status === 409) {
        enqueueSnackbar('Color already exists!', { variant: 'warning' });
      } else {
        enqueueSnackbar('Failed to add color', { variant: 'error' });
      }
    }
  };

  const handleDeleteColor = async (colorToDelete: string) => {
    try {
      const res = await apiClient.delete(`/colors/${colorToDelete}`);
      if (res.data.success) {
        setAvailableColors(prev => prev.filter(c => c !== colorToDelete));
        enqueueSnackbar('Color removed', { variant: 'success' });
      }
    } catch (err) {
      console.error(err);
      enqueueSnackbar('Failed to remove color', { variant: 'error' });
    }
  };

  return (
    <Box>
      <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 3 }}>Manage Dynamic Colors</Typography>
      <Card elevation={2}>
        <CardContent sx={{ p: 4 }}>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
            Colors added here will be used by the OCR engine to dynamically correct misread generic colors.
          </Typography>
          
          <Box sx={{ display: 'flex', gap: 2, mb: 4, maxWidth: 500 }}>
            <TextField
              size="medium"
              label="New Color Name"
              variant="outlined"
              fullWidth
              value={newColorInput}
              onChange={(e) => setNewColorInput(e.target.value.toUpperCase())}
              placeholder="e.g. PEARL VIBRANT BLUE"
            />
            <Button variant="contained" size="large" onClick={handleAddColor} disabled={!newColorInput.trim()}>
              Add Color
            </Button>
          </Box>

          <Typography variant="h6" sx={{ mb: 2, fontWeight: 'bold' }}>Current Registered Colors</Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, minHeight: 100, p: 2, bgcolor: 'background.default', borderRadius: 1 }}>
            {loading ? (
              <CircularProgress size={24} />
            ) : availableColors.length === 0 ? (
              <Typography variant="body2" color="text.secondary">No colors added yet.</Typography>
            ) : (
              availableColors.map((c, idx) => (
                <Chip 
                  key={idx} 
                  label={c} 
                  color="primary" 
                  variant="outlined" 
                  onDelete={() => handleDeleteColor(c)}
                  sx={{ fontWeight: 'bold', fontSize: '0.9rem', py: 2 }} 
                />
              ))
            )}
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
};
