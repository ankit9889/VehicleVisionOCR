import React, { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { Box, Button, Typography, Card, CardContent, CardHeader, CircularProgress, Chip, Divider, Dialog, DialogTitle, DialogContent, TextField, DialogActions } from '@mui/material';
import { apiClient } from '../api/client';
import { useSnackbar } from 'notistack';

import { getBackendBaseUrl } from '../api/client';

// URL for backend SignalR hub
const HUB_URL = `${getBackendBaseUrl()}/hubs/scanner`;

interface ScanResult {
  success: boolean;
  isDuplicate?: boolean;
  scanId?: string;
  vehicleId?: string;
  imageFileName?: string;
  imageId?: string;
  vin?: string;
  registrationNumber?: string;
  color?: string;
  confidence?: number;
  message?: string;
  rawText?: string;
}

let globalScanQueue: ScanResult[] = [];

export const LiveScannerDashboard: React.FC = () => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [currentScan, setCurrentScan] = useState<ScanResult | null>(globalScanQueue.length > 0 ? globalScanQueue[0] : null);
  const [isScanning, setIsScanning] = useState(false);
  
  // Manual Entry State
  const [manualEntryOpen, setManualEntryOpen] = useState(false);
  const [manualBarcode, setManualBarcode] = useState('');
  const [manualColor, setManualColor] = useState('');
  
  // Color Manager State
  const [colorManagerOpen, setColorManagerOpen] = useState(false);
  const [availableColors, setAvailableColors] = useState<string[]>([]);
  const [newColorInput, setNewColorInput] = useState('');

  const { enqueueSnackbar } = useSnackbar();

  useEffect(() => {
    if (colorManagerOpen) {
      apiClient.get('/colors').then(res => setAvailableColors(res.data)).catch(console.error);
    }
  }, [colorManagerOpen]);

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

  const playDuplicateBeep = () => {
    try {
      const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext;
      if (!AudioContextClass) return;
      const audioCtx = new AudioContextClass();
      
      const playNote = (frequency: number, startTime: number, duration: number) => {
        const oscillator = audioCtx.createOscillator();
        const gainNode = audioCtx.createGain();
        
        oscillator.type = 'square';
        oscillator.frequency.setValueAtTime(frequency, audioCtx.currentTime + startTime);
        
        gainNode.gain.setValueAtTime(0.1, audioCtx.currentTime + startTime);
        gainNode.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + startTime + duration);
        
        oscillator.connect(gainNode);
        gainNode.connect(audioCtx.destination);
        
        oscillator.start(audioCtx.currentTime + startTime);
        oscillator.stop(audioCtx.currentTime + startTime + duration);
      };

      // Play 3 short, urgent beeps for duplicate
      playNote(880, 0, 0.15);
      playNote(880, 0.25, 0.15);
      playNote(880, 0.5, 0.15);
    } catch (e) {
      console.error("Audio playback failed", e);
    }
  };

  useEffect(() => {
    // Fetch pending Initiated scans from DB on mount
    apiClient.get('/vehicles/queue').then(res => {
      if (res.data && res.data.length > 0) {
        const existingIds = new Set(globalScanQueue.map(s => s.scanId));
        let added = false;
        res.data.forEach((s: any) => {
          if (!existingIds.has(s.scanId)) {
            globalScanQueue.push(s);
            added = true;
          }
        });
        if (added && !currentScan && globalScanQueue.length > 0) {
          setCurrentScan(globalScanQueue[0]);
        }
      }
    }).catch(err => console.error("Failed to fetch queue", err));

    // Setup SignalR connection to receive live scans
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  const processNextInQueue = () => {
    if (globalScanQueue.length > 0) {
      setCurrentScan(globalScanQueue[0]);
    } else {
      setCurrentScan(null);
    }
  };

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log('Connected to Scanner Hub!');
          
          connection.on('OnScanProcessed', (result: ScanResult) => {
            console.log('Received Scan:', result);
            
            // Prevent double pushes in case of React StrictMode ghost renders
            if (result.scanId && globalScanQueue.some(s => s.scanId === result.scanId)) {
                return;
            }

            globalScanQueue.push(result);
            setIsScanning(false);

            // If we are not currently displaying a scan, show this one immediately
            // Otherwise, it sits in globalScanQueue and waits for handleNext()
            if (globalScanQueue.length === 1) {
              processNextInQueue();
            }

            if (!result.success && result.message?.includes('Duplicate')) {
              playDuplicateBeep();
              enqueueSnackbar('Duplicate Barcode Detected! Added to Queue.', { variant: 'error' });
            } else {
              enqueueSnackbar(`New Scan Received: ${result.registrationNumber || result.vin || 'Data'}. Added to Queue.`, { variant: 'info' });
            }
          });
        })
        .catch(e => console.log('Connection failed: ', e));

      return () => {
        connection.off('OnScanProcessed');
        connection.stop();
      };
    }
  }, [connection]);

  // Keyboard Wedge Interceptor (Fallback for scanners without Zebra SDK)
  useEffect(() => {
    let barcodeBuffer = '';
    let lastKeyTime = Date.now();

    const handleKeyDown = (e: KeyboardEvent) => {
      // Ignore if user is typing in an input field
      if (['INPUT', 'TEXTAREA', 'SELECT'].includes((e.target as HTMLElement).tagName)) {
        return;
      }

      const currentTime = Date.now();
      
      // If time between keys is too long (>50ms), it's a human typing, reset buffer
      if (currentTime - lastKeyTime > 50) {
        barcodeBuffer = '';
      }
      
      lastKeyTime = currentTime;

      if (e.key === 'Enter') {
        if (barcodeBuffer.length > 3) {
          console.log('Intercepted Wedge Scan:', barcodeBuffer);
          const scannedText = barcodeBuffer;
          barcodeBuffer = ''; // Reset immediately
          
          setIsScanning(true);
          
            const newScan = {
              success: true,
              color: 'Unknown',
              registrationNumber: scannedText.toUpperCase(),
              confidence: 100,
              message: 'Captured via USB Keyboard Wedge'
            };
            
            globalScanQueue.push(newScan);
            setIsScanning(false);
            
            if (globalScanQueue.length === 1) {
              processNextInQueue();
            }
            enqueueSnackbar(`Wedge Scan Received: ${scannedText.toUpperCase()}`, { variant: 'info' });
        }
      } else if (e.key.length === 1 && !e.ctrlKey && !e.altKey && !e.metaKey) { 
        barcodeBuffer += e.key;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  const handleOk = async () => {
    try {
      if ((currentScan?.message === 'Manually entered data' || currentScan?.message === 'Captured via USB Keyboard Wedge') && currentScan.registrationNumber) {
        await apiClient.post('/vehicles/manual', {
          color: currentScan.color !== 'Unknown' ? currentScan.color : null,
          registrationNumber: currentScan.registrationNumber || null,
          rawText: currentScan.message === 'Captured via USB Keyboard Wedge' ? 'KEYBOARD WEDGE' : 'MANUAL ENTRY'
        });
        enqueueSnackbar('Manual scan confirmed and saved to history!', { variant: 'success' });
      } else {
        // In this new workflow, we call the API to CONFIRM the queued scan!
        if (currentScan?.vehicleId) {
            await apiClient.post('/vehicles/confirm-scan', { vehicleId: currentScan.vehicleId });
        }
        enqueueSnackbar('Scan confirmed successfully!', { variant: 'success' });
      }
    } catch (err: any) {
      console.error('Failed to save manual scan to DB', err);
      if (err.response?.status === 409) {
        playDuplicateBeep();
        enqueueSnackbar('Duplicate Barcode! Entry rejected.', { variant: 'error' });
        setCurrentScan(prev => prev ? { ...prev, isDuplicate: true, vehicleId: err.response.data.vehicleId } : null);
        return; 
      } else {
        enqueueSnackbar('Failed to save to database', { variant: 'error' });
      }
    }
    
    // Pop queue
    globalScanQueue.shift();
    processNextInQueue();
  };

  const handleUpdateDuplicate = async () => {
    try {
      if (currentScan?.vehicleId) {
        await apiClient.post('/vehicles/update-duplicate', {
          vehicleId: currentScan.vehicleId,
          registrationNumber: currentScan.registrationNumber || null,
          color: currentScan.color || null,
          rawText: currentScan.rawText || 'UPDATED ENTRY',
          imageFileName: currentScan.imageFileName || null
        });
        enqueueSnackbar('Duplicate scan updated successfully!', { variant: 'success' });
      }
    } catch (err) {
      console.error('Failed to update duplicate scan', err);
      enqueueSnackbar('Failed to update duplicate', { variant: 'error' });
    }
    globalScanQueue.shift();
    processNextInQueue();
  };

  const handleCancel = async () => {
    console.log('Cancelled scan:', currentScan?.vin);
    if (currentScan?.scanId) {
       // Delete the unconfirmed scan from DB
       try { await apiClient.delete(`/vehicles/history/${currentScan.scanId}`); } catch {}
    }
    globalScanQueue.shift();
    processNextInQueue();
  };

  const handleNext = () => {
    globalScanQueue.shift();
    processNextInQueue();
  };

  const handleManualSubmit = () => {
    if (!manualBarcode) {
      enqueueSnackbar('Please enter a Barcode', { variant: 'warning' });
      return;
    }
    
    setCurrentScan({
      success: true,
      color: manualColor || 'Unknown',
      registrationNumber: manualBarcode || undefined,
      confidence: 100,
      message: 'Manually entered data'
    });
    setManualEntryOpen(false);
    setManualBarcode('');
    setManualColor('');
  };

  return (
    <Card elevation={2} sx={{ width: '100%', mt: 4, overflow: 'visible' }}>
      <CardHeader 
        title={
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
            <Box 
              sx={{ 
                width: 12, height: 12, borderRadius: '50%', 
                bgcolor: currentScan ? (currentScan.success ? '#4caf50' : '#f44336') : (isScanning ? '#ff9800' : '#2196f3'),
                boxShadow: `0 0 10px ${currentScan ? (currentScan.success ? '#4caf50' : '#f44336') : (isScanning ? '#ff9800' : '#2196f3')}`,
                animation: isScanning ? 'pulse 1s infinite' : 'none',
                '@keyframes pulse': {
                  '0%': { transform: 'scale(0.95)', boxShadow: '0 0 0 0 rgba(255, 152, 0, 0.7)' },
                  '70%': { transform: 'scale(1)', boxShadow: '0 0 0 10px rgba(255, 152, 0, 0)' },
                  '100%': { transform: 'scale(0.95)', boxShadow: '0 0 0 0 rgba(255, 152, 0, 0)' }
                }
              }} 
            />
            <Typography variant="h6" sx={{ fontWeight: 'bold' }}>Live Scan Feed</Typography>
          </Box>
        }
      />
      <Box sx={{ height: '1px', bgcolor: 'divider' }} />
      <CardContent sx={{ p: 0 }}>
        {!currentScan && !isScanning && (
          <Box sx={{ p: 8, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: 300 }}>
            <Typography variant="h5" color="text.secondary" sx={{ mb: 1, fontWeight: 500 }}>
              Ready for Input
            </Typography>
            <Typography variant="body1" color="text.disabled" sx={{ mb: 4 }}>
              Waiting for hardware trigger from Zebra scanner...
            </Typography>
            
            <Divider sx={{ width: '100%', maxWidth: 300, mb: 4 }}>OR</Divider>
            
            <Box sx={{ display: 'flex', gap: 2 }}>
              <Button 
                variant="outlined" 
                color="primary" 
                size="large"
                onClick={() => setManualEntryOpen(true)}
                sx={{ fontWeight: 'bold', px: 4, py: 1.5, borderRadius: 2 }}
              >
                Add Manual Entry
              </Button>
              
              <Button 
                variant="outlined" 
                color="secondary" 
                size="large"
                onClick={() => setColorManagerOpen(true)}
                sx={{ fontWeight: 'bold', px: 4, py: 1.5, borderRadius: 2 }}
              >
                Manage Colors
              </Button>
            </Box>
          </Box>
        )}

        {isScanning && (
          <Box sx={{ p: 8, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: 300 }}>
            <CircularProgress size={60} thickness={4} sx={{ color: 'primary.main', mb: 3 }} />
            <Typography variant="h5" color="text.primary" sx={{ fontWeight: 500 }}>
              Processing Image...
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
              Extracting VIN and vehicle data via Tesseract OCR Engine
            </Typography>
          </Box>
        )}

        {currentScan && (
          <Box sx={{ position: 'relative' }}>
            {/* Top glowing accent line */}
            <Box sx={{ 
              position: 'absolute', top: 0, left: 0, right: 0, height: 4, 
              bgcolor: currentScan.success ? '#4caf50' : '#f44336',
              boxShadow: `0 0 20px ${currentScan.success ? '#4caf50' : '#f44336'}`
            }} />
            
            <Box sx={{ p: 4 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 4 }}>
                <Box>
                  <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 0.5, color: currentScan.success ? 'success.main' : 'error.main' }}>
                    {currentScan.success ? 'Validation Passed' : 'Analysis Failed'}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Processed in real-time via SignalR
                  </Typography>
                </Box>
                <Chip 
                  label={`${currentScan.confidence?.toFixed(1) || 0}% Confidence`}
                  color={currentScan.confidence && currentScan.confidence > 80 ? 'success' : 'warning'} 
                  variant="outlined"
                  sx={{ fontWeight: 'bold', fontSize: '1.1rem', px: 1, py: 2.5, borderRadius: 2 }}
                />
              </Box>

              {(currentScan.imageFileName || currentScan.imageId) && (
                <Box sx={{ mb: 4, textAlign: 'center', bgcolor: '#f5f5f5', borderRadius: 2, p: 2, border: '1px solid #ddd' }}>
                  <img 
                    src={currentScan.imageFileName ? `${getBackendBaseUrl()}/api/vehicles/image/file/${currentScan.imageFileName}` : `${getBackendBaseUrl()}/api/vehicles/image/${currentScan.imageId}`}
                    alt="Vehicle Scan" 
                    style={{ maxHeight: 300, maxWidth: '100%', objectFit: 'contain', borderRadius: '4px' }} 
                  />
                </Box>
              )}

              {currentScan.success ? (
                <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 3, mb: 5 }}>
                  <Box sx={{ p: 3, bgcolor: 'background.paper', borderRadius: 2, border: '1px solid', borderColor: 'divider' }}>
                    <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 'bold', letterSpacing: 1 }}>VIN Number</Typography>
                    <Typography variant="h5" sx={{ fontWeight: 'bold', mt: 0.5 }}>{currentScan.vin || 'N/A'}</Typography>
                  </Box>
                  <Box sx={{ p: 3, bgcolor: 'background.paper', borderRadius: 2, border: '1px solid', borderColor: 'divider' }}>
                    <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 'bold', letterSpacing: 1 }}>Vehicle Color</Typography>
                    <Typography variant="h5" sx={{ fontWeight: 'bold', mt: 0.5 }}>{currentScan.color || 'N/A'}</Typography>
                  </Box>
                  <Box sx={{ p: 3, bgcolor: 'background.paper', borderRadius: 2, border: '1px solid', borderColor: 'divider' }}>
                    <Typography variant="overline" color="text.secondary" sx={{ fontWeight: 'bold', letterSpacing: 1 }}>Registration</Typography>
                    <Typography variant="h5" sx={{ fontWeight: 'bold', mt: 0.5 }}>{currentScan.registrationNumber || 'N/A'}</Typography>
                  </Box>
                </Box>
              ) : (
                <Box sx={{ mb: 5, p: 3, bgcolor: 'error.dark', color: 'error.contrastText', borderRadius: 2 }}>
                  <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 1 }}>Error Message</Typography>
                  <Typography variant="body1">{currentScan.message}</Typography>
                  
                  {currentScan.rawText && (
                    <Box sx={{ mt: 3, p: 2.5, bgcolor: 'rgba(0,0,0,0.3)', borderRadius: 1 }}>
                      <Typography variant="overline" sx={{ fontWeight: 'bold', opacity: 0.8 }}>Raw OCR Extraction</Typography>
                      <Typography variant="body2" sx={{ mt: 1, fontFamily: 'monospace', whiteSpace: 'pre-wrap', lineHeight: 1.6 }}>
                        {currentScan.rawText}
                      </Typography>
                    </Box>
                  )}
                </Box>
              )}

              {currentScan.success && !currentScan.vin && !currentScan.registrationNumber && currentScan.rawText && (
                <Box sx={{ mb: 4, p: 3, bgcolor: '#fff3e0', border: '1px solid #ffcc80', borderRadius: 2 }}>
                  <Typography variant="h6" color="warning.dark" sx={{ mb: 1 }}>Missing Data (Raw OCR Extraction)</Typography>
                  <Typography variant="body1" color="text.secondary" sx={{ mb: 2 }}>
                    The scanner extracted text but couldn't validate the VIN or Registration number.
                  </Typography>
                  <Box sx={{ p: 2, bgcolor: 'rgba(255,255,255,0.7)', borderRadius: 1, fontFamily: 'monospace', whiteSpace: 'pre-wrap' }}>
                    {currentScan.rawText}
                  </Box>
                </Box>
              )}

              <Box sx={{ display: 'flex', gap: 2 }}>
                {!currentScan.isDuplicate ? (
                  <Button 
                    variant="contained" 
                    color="success" 
                    size="large"
                    onClick={handleOk}
                    disabled={!currentScan.success}
                    sx={{ flex: 1, fontWeight: 'bold', py: 2, fontSize: '1.1rem' }}
                  >
                    CONFIRM CHECK-IN
                  </Button>
                ) : (
                  <Button 
                    variant="contained" 
                    color="warning" 
                    size="large"
                    onClick={handleUpdateDuplicate}
                    sx={{ flex: 1, fontWeight: 'bold', py: 2, fontSize: '1.1rem', color: 'black' }}
                  >
                    UPDATE LATEST DATA
                  </Button>
                )}
                
                <Button 
                  variant="outlined" 
                  color="error" 
                  size="large"
                  onClick={handleCancel}
                  sx={{ flex: 1, fontWeight: 'bold', py: 2, fontSize: '1.1rem', borderWidth: 2, '&:hover': { borderWidth: 2 } }}
                >
                  {currentScan.isDuplicate ? 'REJECT DUPLICATE' : 'CANCEL / DISCARD'}
                </Button>
                
                <Button 
                  variant="contained" 
                  color="primary" 
                  size="large"
                  onClick={handleNext}
                  sx={{ flex: 1, fontWeight: 'bold', py: 2, fontSize: '1.1rem' }}
                >
                  NEW SCAN
                </Button>
              </Box>
            </Box>
          </Box>
        )}
      </CardContent>

      {/* Manual Entry Dialog */}
      <Dialog open={manualEntryOpen} onClose={() => setManualEntryOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontWeight: 'bold' }}>Manual Vehicle Entry</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="Barcode (Registration)"
            type="text"
            fullWidth
            variant="outlined"
            value={manualBarcode}
            onChange={(e) => setManualBarcode(e.target.value.toUpperCase())}
          />
          <TextField
            margin="dense"
            label="Vehicle Color"
            type="text"
            fullWidth
            variant="outlined"
            value={manualColor}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => setManualColor(e.target.value)}
          />
        </DialogContent>
        <DialogActions sx={{ p: 3, pt: 0 }}>
          <Button onClick={() => setManualEntryOpen(false)} color="inherit" sx={{ fontWeight: 'bold' }}>
            Cancel
          </Button>
          <Button onClick={handleManualSubmit} variant="contained" disabled={!manualBarcode} sx={{ fontWeight: 'bold', px: 3 }}>
            Submit Entry
          </Button>
        </DialogActions>
      </Dialog>

      {/* Color Manager Dialog */}
      <Dialog open={colorManagerOpen} onClose={() => setColorManagerOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontWeight: 'bold' }}>Manage Database Colors</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            These colors will be used by the OCR engine to dynamically correct misread generic colors.
          </Typography>
          
          <Box sx={{ display: 'flex', gap: 1, mb: 3 }}>
            <TextField
              size="small"
              label="New Color Name"
              variant="outlined"
              fullWidth
              value={newColorInput}
              onChange={(e) => setNewColorInput(e.target.value.toUpperCase())}
              placeholder="e.g. PEARL VIBRANT BLUE"
            />
            <Button variant="contained" onClick={handleAddColor} disabled={!newColorInput.trim()}>
              Add
            </Button>
          </Box>

          <Typography variant="subtitle2" sx={{ mb: 1, fontWeight: 'bold' }}>Current Dynamic Colors:</Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, maxHeight: 300, overflowY: 'auto', p: 1, border: '1px solid #eee', borderRadius: 1 }}>
            {availableColors.length === 0 ? (
              <Typography variant="body2" color="text.secondary">No colors added yet.</Typography>
            ) : (
              availableColors.map((c, idx) => (
                <Chip 
                  key={idx} 
                  label={c} 
                  size="small" 
                  onDelete={() => handleDeleteColor(c)}
                  sx={{ fontWeight: 'bold' }} 
                />
              ))
            )}
          </Box>
        </DialogContent>
        <DialogActions sx={{ p: 3, pt: 0 }}>
          <Button onClick={() => setColorManagerOpen(false)} color="inherit" sx={{ fontWeight: 'bold' }}>
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Card>
  );
};
