import React, { useState, useEffect } from 'react';
import { 
  Box, Button, Typography, Paper, MenuItem, 
  Select, FormControl, InputLabel, TextField, Stack, Divider, Slider,
  Dialog, DialogTitle, DialogContent, DialogActions 
} from '@mui/material';

// Ikony
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import StopIcon from '@mui/icons-material/Stop'; 
import FileUploadIcon from '@mui/icons-material/FileUpload';
import DownloadIcon from '@mui/icons-material/Download'; 

// Importy služeb a hooků
import { ControlsService, SessionsService } from '../client'; 
import { useConfigManager } from '../hooks/useConfigManager';
import { useSessionExport } from '../hooks/useSessionExport'; 

interface ControlPanelProps {
  deviceId: string;
  patientId: number | '';
  onSessionStateChange?: (isActive: boolean) => void;
}

const ControlPanel = ({ deviceId, patientId, onSessionStateChange }: ControlPanelProps) => {
  const [gameType, setGameType] = useState<string>('rotation_cube');
  const [schema, setSchema] = useState<any>(null);
  const [payload, setPayload] = useState<any>({ difficulty: 1, iterations: 1 });
  const [loading, setLoading] = useState(true);
  
  const [activeSessionId, setActiveSessionId] = useState<number | null>(null);

  const [commandHistory, setCommandHistory] = useState<any[]>([]);

  const [isEndModalOpen, setEndModalOpen] = useState(false); 
  const { downloadSessionZip, isExporting } = useSessionExport(); 

  const { handleImport } = useConfigManager(schema, setGameType, setPayload);

  useEffect(() => {
    const fetchSchema = async () => {
      try {
        setLoading(true);
        const data = await ControlsService.getGameSchemaApiGameSchemaGet();
        setSchema(data);
      } catch (error) {
        console.error("❌ Chyba při načítání schématu:", error);
      } finally {
        setLoading(false);
      }
    };
    fetchSchema();
  }, []);

  const getCurrentGameProperties = () => {
    if (!schema) return null;

    // 1. Zkusíme najít složité schéma (s $defs) - pro budoucí škálování
    if (schema.$defs) {
      const foundDefKey = Object.keys(schema.$defs).find(key => {
        const props = schema.$defs[key].properties;
        return props?.game_type?.default === gameType || props?.game_type?.const === gameType;
      });
      if (foundDefKey) return schema.$defs[foundDefKey].properties;
    }

    // 2. Fallback: Pokud je schéma "ploché" (zjednodušené modely), použijeme ho rovnou
    if (schema.properties) {
      return schema.properties;
    }

    return null;
  };

  const properties = getCurrentGameProperties();

  // Dynamické sestavení konfigurace před odesláním
  const getCompleteConfig = () => {
    const complete: any = { game_type: gameType };
    if (properties) {
      Object.entries(properties).forEach(([key, fieldData]: [string, any]) => {
        if (key === 'game_type') return;
        const userValue = payload[key];
        complete[key] = userValue !== undefined ? userValue : fieldData.default;
      });
    } else {
      return { game_type: gameType, ...payload };
    }
    return complete;
  };

  const handleStartSession = async () => {
    try {
      const response = await SessionsService.startSessionApiSessionsPost({
        patient_id: Number(patientId),
        device_id: deviceId
      });
      
      setActiveSessionId(response.id);
      
      if (onSessionStateChange) {
        onSessionStateChange(true);
      }

    } catch (error: any) {
      console.error("Failed to start session:", error);
      const statusCode = error.status || error.response?.status;
      if (statusCode === 409) {
        alert("⚠️ NELZE SPUSTIT: Zařízení nebo pacient jsou právě obsazeni v jiné terapii.");
      } else {
        alert("Nepodařilo se zahájit session. Zkontrolujte připojení k serveru.");
      }
    }
  };

  const handleLaunchGame = async () => {
    try {
      if (!activeSessionId) {
        alert("Nejprve musíte zahájit sezení!");
        return;
      }

      const currentConfig = getCompleteConfig();

      await ControlsService.sendCommandToUnityApiGameCommandPost(
        activeSessionId, 
        deviceId, 
        { payload: currentConfig as any }
      );
      
      const historyItem = {
        timestamp: new Date().toISOString(),
        action: "UPDATE",
        ...currentConfig 
      };
      setCommandHistory(prev => [...prev, historyItem]);
      console.log("📝 Příkaz odeslán a přidán do historie:", historyItem);

    } catch (error) {
      console.error("❌ Chyba při odesílání do headsetu:", error);
      alert("Chyba při komunikaci s brýlemi.");
    }
  };

  const handleStopClick = () => {
    setEndModalOpen(true);
  };

  const confirmEndAndExport = async () => {
    if (activeSessionId) {
      const dataToExport = commandHistory.length > 0 
        ? commandHistory 
        : [{ timestamp: new Date().toISOString(), action: "MANUAL_EXPORT", game_type: gameType, ...payload }];

      await downloadSessionZip(activeSessionId, dataToExport);
    }
    await handleReallyEndSession();
    setEndModalOpen(false);
  };

  const handleReallyEndSession = async () => {
    try {
      if (activeSessionId) {
        await SessionsService.stopSessionApiSessionsSessionIdEndPost(activeSessionId);
      }
      setActiveSessionId(null);
      setCommandHistory([]); 
    } catch (error) {
      console.error("❌ Chyba při ukončování:", error);
    }

    if (onSessionStateChange) {
        onSessionStateChange(false);
      }
  };

  const renderField = (key: string, fieldData: any) => {
    if (key === 'game_type') return null;
    const label = fieldData.label || key;
    const uiType = fieldData.ui_type || 'number';
    const value = payload[key] ?? fieldData.default;

    return (
      <Box key={key} sx={{ mb: 2 }}>
        <Typography variant="caption" sx={{ color: '#8b949e', textTransform: 'uppercase', fontWeight: 'bold' }}>
          {label}
        </Typography>
        {uiType === 'slider' ? (
          <Slider
            value={value} min={fieldData.minimum || 1} max={fieldData.maximum || 5} step={fieldData.step || 1} marks
            onChange={(_, val) => setPayload({ ...payload, [key]: val })}
            valueLabelDisplay="auto" sx={{ color: key === 'difficulty' ? '#58a6ff' : '#58a6ff' }}
          />
        ) : uiType === 'select' ? (
          <Select
            fullWidth size="small" value={value}
            onChange={(e) => setPayload({ ...payload, [key]: e.target.value })}
            sx={{ color: 'white', bgcolor: '#0d1117' }}
          >
            {fieldData.options?.map((opt: any) => (
              <MenuItem key={opt} value={opt}>{opt}</MenuItem>
            ))}
          </Select>
        ) : (
          <TextField
            fullWidth type="number" size="small" value={value}
            onChange={(e) => setPayload({ ...payload, [key]: Number(e.target.value) })}
            sx={{ 
              input: { color: 'white' }, bgcolor: '#0d1117',
              '& .MuiOutlinedInput-notchedOutline': { borderColor: '#30363d' } 
            }}
          />
        )}
      </Box>
    );
  };

  if (loading) return <Typography sx={{ color: 'white', p: 2 }}>Načítám konfiguraci...</Typography>;

  const isReadyToDeploy = Boolean(deviceId && patientId !== '');

  return (
    <Paper sx={{ p: 2, bgcolor: '#161b22', border: '1px solid #30363d', borderRadius: 2, height: '100%', color: 'white' }}>
      
      <Divider sx={{ mb: 2, borderColor: '#30363d' }} />

      <Stack spacing={2}>
        <Button
            variant="outlined" component="label" fullWidth size="small"
            startIcon={<FileUploadIcon />}
            sx={{ borderColor: '#30363d', color: '#8b949e', textTransform: 'none' }}
        >
            Import Preset JSON
            <input type="file" hidden accept=".json" onChange={handleImport} />
        </Button>

        <Divider sx={{ mb: 1, borderColor: '#30363d', borderStyle: 'dashed' }} />

        <FormControl fullWidth size="small">
          <InputLabel sx={{ color: '#8b949e' }}>Active Module</InputLabel>
          <Select
            value={gameType} label="Active Module"
            onChange={(e) => {
              setGameType(e.target.value);
              // Při změně hry vyresetujeme payload na bezpečné výchozí hodnoty
              setPayload({ difficulty: 1, iterations: 1 });
            }}
            sx={{ color: 'white', '.MuiOutlinedInput-notchedOutline': { borderColor: '#30363d' } }}
          >
            <MenuItem value="rotation_cube">Rotation Cube</MenuItem>
            <MenuItem value="corsi_blocks">Corsi Blocks</MenuItem>
            <MenuItem value="location_recall">Location Recall</MenuItem>
            <MenuItem value="attention_tracking">Attention Tracking</MenuItem>
            <MenuItem value="mr_puzzle">Mr. Puzzle</MenuItem>
          </Select>
        </FormControl>

        <Box sx={{ bgcolor: '#0d1117', p: 2, borderRadius: 1, border: '1px solid #30363d' }}>
          {properties ? (
            Object.entries(properties).map(([key, data]) => renderField(key, data))
          ) : (
            <Typography variant="caption" color="error">Nepodařilo se naparsovat schéma z backendu.</Typography>
          )}
        </Box>

        {!isReadyToDeploy ? (
          <Typography variant="caption" color="warning.main" align="center">
            Nejprve vyberte pacienta a zařízení v horní liště.
          </Typography>
        ) : !activeSessionId ? (
          <Button 
            variant="contained" 
            fullWidth
            onClick={handleStartSession}
            sx={{ bgcolor: '#238636', '&:hover': { bgcolor: '#2ea043' }, fontWeight: 'bold', py: 1.5 }}
          >
            START SESSION
          </Button>
        ) : (
          <Stack spacing={2}>
            <Button 
              variant="contained" 
              fullWidth
              startIcon={<PlayArrowIcon />}
              onClick={handleLaunchGame}
              sx={{ bgcolor: '#1f6feb', '&:hover': { bgcolor: '#388bfd' }, fontWeight: 'bold', py: 1.5 }}
            >
              DEPLOY TO HEADSET
            </Button>

            <Button 
              variant="contained" 
              fullWidth
              startIcon={<StopIcon />}
              onClick={handleStopClick}
              sx={{ bgcolor: '#da3633', '&:hover': { bgcolor: '#b62324' }, fontWeight: 'bold', py: 1.5 }}
            >
              STOP SESSION (ID: {activeSessionId})
            </Button>
          </Stack>
        )}
      </Stack>

      {/* --- EXPORTNÍ MODAL --- */}
      <Dialog 
        open={isEndModalOpen} 
        onClose={() => setEndModalOpen(false)}
        PaperProps={{ style: { backgroundColor: '#161b22', color: '#fff', border: '1px solid #30363d', minWidth: '400px' } }}
      >
        <DialogTitle sx={{ borderBottom: '1px solid #30363d' }}>Ukončit sezení?</DialogTitle>
        <DialogContent sx={{ mt: 2 }}>
            <Typography>Session <strong>{activeSessionId}</strong> bude ukončena.</Typography>
            <Typography sx={{ mt: 1, color: '#aaa', fontSize: '0.9rem' }}>Chcete stáhnout kompletní ZIP záznam (Logy + Historie konfigurace)?</Typography>
        </DialogContent>
        <DialogActions sx={{ p: 2, borderTop: '1px solid #30363d' }}>
            <Button onClick={() => setEndModalOpen(false)} sx={{ color: '#aaa' }}>Zrušit</Button>
            <Button onClick={async () => { await handleReallyEndSession(); setEndModalOpen(false); }} color="error">Ukončit bez uložení</Button>
            <Button 
                onClick={confirmEndAndExport} 
                variant="contained" 
                color="primary"
                startIcon={<DownloadIcon />}
                disabled={isExporting}
            >
                {isExporting ? "Stahuji..." : "Uložit ZIP a Ukončit"}
            </Button>
        </DialogActions>
      </Dialog>
    </Paper>
  );
};

export default ControlPanel;