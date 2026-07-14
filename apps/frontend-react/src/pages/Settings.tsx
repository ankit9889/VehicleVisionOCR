import React from 'react';
import { Box, Card, CardContent, Typography, Switch, FormControlLabel, Select, MenuItem, Button, Divider } from '@mui/material';
import { useSettingsStore } from '../stores/settingsStore';

export const Settings: React.FC = () => {
  const { theme, setTheme, language, setLanguage } = useSettingsStore();

  return (
    <Box>
      <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 3 }}>Application Settings</Typography>

      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3 }}>
        <Box sx={{ flex: '1 1 350px' }}>
          <Card elevation={2}>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>Appearance</Typography>
              <Box sx={{ mb: 2 }}>
                <FormControlLabel
                  control={<Switch checked={theme === 'dark'} onChange={(e) => setTheme(e.target.checked ? 'dark' : 'light')} />}
                  label="Dark Theme"
                />
              </Box>
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle2" sx={{ mb: 1 }}>Language</Typography>
                <Select value={language} onChange={(e) => setLanguage(e.target.value as string)} size="small" fullWidth>
                  <MenuItem value="en-US">English (US)</MenuItem>
                  <MenuItem value="es-ES">Español</MenuItem>
                  <MenuItem value="fr-FR">Français</MenuItem>
                </Select>
              </Box>
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: '1 1 350px' }}>
          <Card elevation={2}>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>System Management</Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                Manage offline database backups and storage settings.
              </Typography>
              <Divider sx={{ mb: 2 }} />
              <Box sx={{ display: 'flex', gap: 2 }}>
                <Button variant="contained">Backup Database</Button>
                <Button variant="outlined" color="error">Clear Temp Storage</Button>
              </Box>
            </CardContent>
          </Card>
        </Box>
      </Box>
    </Box>
  );
};
