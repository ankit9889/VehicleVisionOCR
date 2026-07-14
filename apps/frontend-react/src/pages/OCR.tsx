import React, { useState } from 'react';
import { Box, Button, Card, CardContent, CardHeader, Divider, Typography, TextField, CircularProgress, Chip } from '@mui/material';
import { useOcrStore } from '../stores/ocrStore';
import { apiClient } from '../api/client';
import { useSnackbar } from 'notistack';

export const OCR: React.FC = () => {
  const { lastImage, lastResult, isProcessing, setProcessing, setResult, setLastImage } = useOcrStore();
  const { enqueueSnackbar } = useSnackbar();
  const [vinEdit, setVinEdit] = useState('');
  const [colorEdit, setColorEdit] = useState('');
  const [modelEdit, setModelEdit] = useState('');

  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onloadend = () => {
        setLastImage(reader.result as string);
        setResult(null);
      };
      reader.readAsDataURL(file);
    }
  };

  const processImage = async () => {
    if (!lastImage) return;
    try {
      setProcessing(true);
      const base64Data = lastImage.split(',')[1];
      const response = await apiClient.post('/ocr/process', { base64Image: base64Data });
      setResult(response.data.result);
      
      const vinField = response.data.result?.extractedFields?.find((f: any) => f.key === 'VIN');
      const colorField = response.data.result?.extractedFields?.find((f: any) => f.key === 'Color');
      const modelField = response.data.result?.extractedFields?.find((f: any) => f.key === 'Model');
      
      setVinEdit(vinField?.value || '');
      setColorEdit(colorField?.value || '');
      setModelEdit(modelField?.value || '');
      
      enqueueSnackbar('OCR Processing Complete', { variant: 'success' });
    } catch (err) {
      enqueueSnackbar('OCR Processing Failed', { variant: 'error' });
    } finally {
      setProcessing(false);
    }
  };

  return (
    <Box>
      <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 3 }}>OCR Processing</Typography>

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3 }}>
        <Box sx={{ flex: '1 1 500px' }}>
          <Card elevation={2}>
            <CardHeader title="Image Viewer" action={
              <Button variant="outlined" component="label">
                Upload Image
                <input type="file" hidden accept="image/*" onChange={handleFileUpload} />
              </Button>
            } />
            <Divider />
            <CardContent>
              {lastImage ? (
                <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
                  <img src={lastImage} alt="Captured" style={{ maxWidth: '100%', maxHeight: '500px', objectFit: 'contain' }} />
                  <Button variant="contained" sx={{ mt: 3 }} onClick={processImage} disabled={isProcessing}>
                    {isProcessing ? <CircularProgress size={24} /> : 'Process OCR'}
                  </Button>
                </Box>
              ) : (
                <Box sx={{ height: 300, display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'background.default', borderRadius: 1 }}>
                  <Typography color="text.secondary">No image available. Capture or upload an image.</Typography>
                </Box>
              )}
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: '1 1 350px' }}>
          <Card elevation={2}>
            <CardHeader title="Extracted Data" />
            <Divider />
            <CardContent>
              {!lastResult && !isProcessing && (
                <Typography color="text.secondary">Run OCR to extract fields.</Typography>
              )}
              {isProcessing && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, p: 2 }}>
                  <CircularProgress size={20} />
                  <Typography>Analyzing image...</Typography>
                </Box>
              )}
              {lastResult && (
                <Box>
                  <Box sx={{ mb: 3 }}>
                    <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>Overall Confidence</Typography>
                    <Chip 
                      label={`${(lastResult.overallConfidence?.percentage || 0).toFixed(1)}%`} 
                      color={lastResult.overallConfidence?.isReliable ? 'success' : 'warning'} 
                    />
                  </Box>

                  <Box sx={{ mb: 2 }}>
                    <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>VIN (Vehicle Identification Number)</Typography>
                    <TextField 
                      fullWidth 
                      value={vinEdit} 
                      onChange={(e) => setVinEdit(e.target.value)}
                      variant="outlined"
                      size="small"
                    />
                  </Box>

                  <Box sx={{ mb: 2 }}>
                    <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>Model / ID</Typography>
                    <TextField 
                      fullWidth 
                      value={modelEdit} 
                      onChange={(e) => setModelEdit(e.target.value)}
                      variant="outlined"
                      size="small"
                    />
                  </Box>

                  <Box sx={{ mb: 3 }}>
                    <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>Color</Typography>
                    <TextField 
                      fullWidth 
                      value={colorEdit} 
                      onChange={(e) => setColorEdit(e.target.value)}
                      variant="outlined"
                      size="small"
                    />
                  </Box>

                  <Divider sx={{ my: 2 }} />
                  
                  <Box sx={{ mb: 3 }}>
                    <Typography variant="subtitle2" color="text.secondary" sx={{ mb: 1 }}>Raw Extracted Text</Typography>
                    <TextField 
                      fullWidth 
                      multiline
                      rows={4}
                      value={lastResult.rawText || ''} 
                      slotProps={{ htmlInput: { readOnly: true } }}
                      variant="outlined"
                      size="small"
                      sx={{ bgcolor: 'background.paper' }}
                    />
                  </Box>

                  <Box sx={{ display: 'flex', gap: 2, mt: 4 }}>
                    <Button 
                      variant="contained" 
                      color="success" 
                      fullWidth
                      onClick={async () => {
                        try {
                          await apiClient.post('/vehicles/manual', {
                            vin: vinEdit,
                            registrationNumber: modelEdit, // Mapping Model/ID to RegistrationNumber
                            color: colorEdit,
                            rawText: lastResult?.rawText
                          });
                          enqueueSnackbar('Data approved and saved successfully', { variant: 'success' });
                          setResult(null);
                          setLastImage(null);
                        } catch (err) {
                          console.error('Failed to save OCR data:', err);
                          enqueueSnackbar('Failed to save data. Check if barcode is duplicate.', { variant: 'error' });
                        }
                      }}
                    >
                      Approve & Save
                    </Button>
                    <Button 
                      variant="outlined" 
                      color="error" 
                      fullWidth
                      onClick={() => {
                        enqueueSnackbar('OCR result rejected', { variant: 'warning' });
                        setResult(null);
                        setLastImage(null);
                      }}
                    >
                      Reject
                    </Button>
                  </Box>
                </Box>
              )}
            </CardContent>
          </Card>
        </Box>
      </Box>
    </Box>
  );
};
