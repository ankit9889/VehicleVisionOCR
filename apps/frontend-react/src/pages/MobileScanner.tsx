import React, { useRef, useState } from 'react';
import { Box, Button, Typography, CircularProgress } from '@mui/material';
import { CameraAlt as CameraIcon } from '@mui/icons-material';
import { enqueueSnackbar } from 'notistack';

export const MobileScanner: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleCapture = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    try {
      setLoading(true);
      
      // Client-side image resizing
      const resizedBlob = await new Promise<Blob>((resolve, reject) => {
        const img = new Image();
        img.onload = () => {
          const canvas = document.createElement('canvas');
          const MAX_WIDTH = 1280;
          let width = img.width;
          let height = img.height;
          
          if (width > MAX_WIDTH) {
            height = height * (MAX_WIDTH / width);
            width = MAX_WIDTH;
          }
          
          canvas.width = width;
          canvas.height = height;
          const ctx = canvas.getContext('2d');
          ctx?.drawImage(img, 0, 0, width, height);
          
          canvas.toBlob((blob) => {
            if (blob) resolve(blob);
            else reject(new Error('Canvas to Blob failed'));
          }, 'image/jpeg', 0.8);
        };
        img.onerror = () => reject(new Error('Failed to load image'));
        img.src = URL.createObjectURL(file);
      });

      const formData = new FormData();
      formData.append('image', resizedBlob, file.name);

      const apiUrl = `http://${window.location.hostname}:5256/api/mobilescanner/upload`;
      const response = await fetch(apiUrl, {
        method: 'POST',
        body: formData,
      });

      if (!response.ok) {
        const errText = await response.text();
        throw new Error(errText || 'Upload failed');
      }

      enqueueSnackbar('Image processed successfully!', { variant: 'success' });
    } catch (err: any) {
      enqueueSnackbar(err.message || 'Error uploading image', { variant: 'error' });
    } finally {
      setLoading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  return (
    <Box sx={{ 
      display: 'flex', 
      flexDirection: 'column', 
      alignItems: 'center', 
      justifyContent: 'center', 
      height: '100vh',
      bgcolor: 'background.default',
      p: 3
    }}>
      <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 1, textAlign: 'center' }}>
        Mobile Web Scanner
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4, textAlign: 'center' }}>
        Use your phone's camera to scan vehicles. Connect to this device on the desktop UI first!
      </Typography>

      <input
        type="file"
        accept="image/*"
        capture="environment"
        style={{ display: 'none' }}
        ref={fileInputRef}
        onChange={handleCapture}
      />

      <Button
        variant="contained"
        color="primary"
        size="large"
        startIcon={loading ? <CircularProgress size={24} color="inherit" /> : <CameraIcon />}
        onClick={() => fileInputRef.current?.click()}
        disabled={loading}
        sx={{
          borderRadius: 8,
          py: 2,
          px: 6,
          fontSize: '1.2rem',
          textTransform: 'none'
        }}
      >
        {loading ? 'Processing...' : 'Take Photo'}
      </Button>
    </Box>
  );
};
