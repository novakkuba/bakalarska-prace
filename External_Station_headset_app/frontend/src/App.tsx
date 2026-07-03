import { useState, useEffect, useRef } from 'react';
import {
  AppBar, Toolbar,Button, Typography, Autocomplete,
  Chip, Box, CssBaseline, ThemeProvider, createTheme,
  FormControl, InputLabel, Select, MenuItem,
  Dialog, DialogTitle, DialogContent, DialogActions, TextField, IconButton
} from '@mui/material';

// Ikony
import BoltIcon from '@mui/icons-material/Bolt';
import VideocamIcon from '@mui/icons-material/Videocam';
import FullscreenIcon from '@mui/icons-material/Fullscreen';
import FullscreenExitIcon from '@mui/icons-material/FullscreenExit';
import FiberManualRecordIcon from '@mui/icons-material/FiberManualRecord';
import StopIcon from '@mui/icons-material/Stop';
import TerminalIcon from '@mui/icons-material/Terminal';
import GamepadIcon from '@mui/icons-material/Gamepad';
import Tooltip from '@mui/material/Tooltip';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import AddIcon from '@mui/icons-material/Add'; // 👇 NOVÁ IKONA PRO PŘIDÁNÍ PACIENTA

import { DevicesService, PatientsService } from './client';

import { BACKEND_URL } from './config';
import ControlPanel from './components/ControlPanel';

import { useRecorder } from './hooks/useRecorder';
import { useWebRTC } from './hooks/useWebRTC';

const darkTheme = createTheme({
  palette: {
    mode: 'dark',
    primary: { main: '#64b5f6' },
    secondary: { main: '#f48fb1' },
    background: { default: '#0d1117', paper: '#161b22' },
  },
  typography: {
    fontFamily: '"Consolas", "Roboto Mono", "Courier New", monospace',
  },
});

function App() {
  const [selectedDeviceId, setSelectedDeviceId] = useState<string>('');
  const [selectedPatientId, setSelectedPatientId] = useState<number | ''>('');
  
  const [onlineDevices, setOnlineDevices] = useState<any[]>([]);
  const [patients, setPatients] = useState<any[]>([]);

  const [isPatientModalOpen, setPatientModalOpen] = useState(false);
  const [newPatientName, setNewPatientName] = useState("");
  const [isCreatingPatient, setIsCreatingPatient] = useState(false);

  const [isSessionActive, setIsSessionActive] = useState(false);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const devicesFromApi = await DevicesService.getOnlineDevicesApiDevicesDevicesOnlineGet();
        const patientsArray = await PatientsService.readPatientsApiPatientsPatientsGet();
        
        const formattedDevices = devicesFromApi.map((d: any) => ({ 
          id: d.hardware_id,
          name: d.name || d.hardware_id 
        }));
        
        setOnlineDevices((prevDevices) => {
            if (selectedDeviceId && !formattedDevices.find((d: any) => d.id === selectedDeviceId)) {
                const currentlySelected = prevDevices.find(d => d.id === selectedDeviceId);
                if (currentlySelected) {
                    return [...formattedDevices, currentlySelected];
                }
            }
            return formattedDevices;
        });
        
        setPatients(patientsArray.map((p: any) => ({ id: p.id, name: p.patient_code })));
      } catch (err) {
        console.error("Nepodařilo se načíst data z DB:", err);
      }
    };

    fetchData(); 
    const interval = setInterval(fetchData, 5000); 
    
    return () => clearInterval(interval);
    

  }, [selectedDeviceId]);

  const [liveStream, setLiveStream] = useState<any[]>([]);
  const [streamStatus, setStreamStatus] = useState<string>("Čekání na zařízení...");
  const [isFullscreen, setIsFullscreen] = useState(false);
  
  const [incomingWebrtcSignal, setIncomingWebrtcSignal] = useState<any>(null);
  
  const liveEndRef = useRef<HTMLDivElement>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  const videoContainerRef = useRef<HTMLDivElement>(null);
  
  const { isRecording, startRecording, stopRecording } = useRecorder(videoRef);
  const { videoStatus } = useWebRTC(videoRef, selectedDeviceId, incomingWebrtcSignal);

  const toggleFullscreen = () => {
    if (!document.fullscreenElement) {
      videoContainerRef.current?.requestFullscreen().catch(err => {
        console.error(`Error attempting to enable fullscreen: ${err.message}`);
      });
    } else {
      document.exitFullscreen();
    }
  };

  useEffect(() => {
    const handleFullscreenChange = () => setIsFullscreen(!!document.fullscreenElement);
    document.addEventListener('fullscreenchange', handleFullscreenChange);
    return () => document.removeEventListener('fullscreenchange', handleFullscreenChange);
  }, []);

  const handleCreatePatient = async () => {
    if (!newPatientName.trim()) return;

    try {
      setIsCreatingPatient(true);
      
      const newPatient = await PatientsService.createPatientApiPatientsPatientsPost({
        patient_code: newPatientName
      });

      // Okamžité přidání do lokálního stavu 
      setPatients(prev => [...prev, { id: newPatient.id, name: newPatient.patient_code }]);
      
      // Automatický výběr nového pacienta v roletce
      setSelectedPatientId(newPatient.id);
      
      // Úklid a zavření okna
      setNewPatientName("");
      setPatientModalOpen(false);

    } catch (error) {
      console.error("❌ Chyba při vytváření pacienta:", error);
      alert("Nepodařilo se vytvořit pacienta.");
    } finally {
      setIsCreatingPatient(false);
    }
  };

  useEffect(() => {
    if (!selectedDeviceId) {
      setStreamStatus("Čekání na zařízení...");
      setLiveStream([]); 
      setIncomingWebrtcSignal(null);
      return;
    }

    const eventSource = new EventSource(`${BACKEND_URL}/api/stream/${selectedDeviceId}`);
    
    eventSource.onopen = () => setStreamStatus("Connected");
    
    eventSource.onmessage = (event) => {
      try {
        const parsedData = JSON.parse(event.data);
        
        if (parsedData.type === "webrtc") {
            setIncomingWebrtcSignal(parsedData.payload); 
        } else {
            setLiveStream((prev) => [...prev, parsedData].slice(-50));
        }
      } catch (err) { 
        console.error("Chyba při zpracování zprávy ze streamu:", err); 
      }
    };
    
    eventSource.onerror = () => {
      setStreamStatus("Reconnecting...");
      eventSource.close();
    };
    
    return () => eventSource.close();
  }, [selectedDeviceId]);

  // Automatické scrollování logů dolů
  useEffect(() => {
    liveEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [liveStream]);

  const LogCard = ({ topic, payload, color }: any) => (
    <Card sx={{ mb: 1, bgcolor: '#1e242e', borderLeft: `3px solid ${color}`, borderRadius: 0 }}>
      <CardContent sx={{ p: '6px 10px !important' }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
            <Typography variant="subtitle2" sx={{ color: color, fontWeight: 'bold', fontSize: '0.75rem' }}>
            {topic}
            </Typography>
            <Typography variant="caption" sx={{ color: '#555', fontSize: '0.7rem' }}>
                {new Date().toLocaleTimeString()}
            </Typography>
        </Box>
        <Typography variant="body2" sx={{ color: '#d1d5db', wordBreak: 'break-all', fontFamily: 'monospace', fontSize: '0.75rem', lineHeight: 1.2 }}>
          {JSON.stringify(payload)}
        </Typography>
      </CardContent>
    </Card>
  );

  return (
    <ThemeProvider theme={darkTheme}>
      <CssBaseline />

      <Box sx={{ display: 'flex', flexDirection: 'column', height: '100vh', width: '100vw', overflow: 'hidden', bgcolor: '#000' }}>

        {/* --- HEADER --- */}
        <AppBar position="static" color="transparent" elevation={0} sx={{ borderBottom: '1px solid #30363d', height: '64px', flexShrink: 0, bgcolor: '#0d1117' }}>
          <Toolbar variant="dense" sx={{ height: '100%', gap: 3 }}>
            <Box sx={{ display: 'flex', alignItems: 'center' }}>
              <BoltIcon sx={{ mr: 1, color: '#f48fb1' }} />
              <Typography variant="h6" sx={{ letterSpacing: 1, mr: 3 }}>
                Asistenční aplikace
              </Typography>
            </Box>

            {/* VÝBĚR PACIENTA S TLAČÍTKEM PLUS */}
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Autocomplete
                size="small"
                disabled={isSessionActive}
                options={patients}
                getOptionLabel={(option) => option.name}
                value={patients.find(p => p.id === selectedPatientId) || null}
                onChange={(event, newValue) => {
                  setSelectedPatientId(newValue ? newValue.id : '');
                }}
                sx={{ minWidth: 200 }}
                renderInput={(params) => (
                  <TextField 
                    {...params} 
                    label="Pacient" 
                    sx={{ 
                      bgcolor: '#161b22', 
                      label: { color: '#8b949e' },
                      input: { color: 'white' },
                      '& .MuiOutlinedInput-root': { 
                        '& fieldset': { borderColor: '#30363d' },
                        '&:hover fieldset': { borderColor: '#58a6ff' }
                      } 
                    }} 
                  />
                )}
                componentsProps={{
                  paper: { sx: { bgcolor: '#161b22', color: 'white', border: '1px solid #30363d' } }
                }}
              />
              
              {/* Tlačítko Přidat */}
              <IconButton 
                onClick={() => setPatientModalOpen(true)}
                disabled={isSessionActive}
                sx={{ 
                  bgcolor: '#238636', color: 'white', '&:hover': { bgcolor: '#2ea043' }, 
                  width: 36, height: 36, borderRadius: 1,
                  '&.Mui-disabled': { bgcolor: '#1e2e22', color: '#8b949e' } 
                }}
              >
                <AddIcon />
              </IconButton>
            </Box>

            {/* VÝBĚR ZAŘÍZENÍ */}
            <FormControl size="small" sx={{ minWidth: 200 }}>
              <InputLabel sx={{ color: '#8b949e' }}>Online Headset</InputLabel>
              <Select
                value={selectedDeviceId}
                disabled={isSessionActive}
                label="Online Headset"
                onChange={(e) => setSelectedDeviceId(e.target.value as string)}
                sx={{ color: 'white', bgcolor: '#161b22', '.MuiOutlinedInput-notchedOutline': { borderColor: '#30363d' } }}
              >
                {/* PŘIDANÁ MOŽNOST PRO VYNULOVÁNÍ VÝBĚRU */}
                <MenuItem value="">
                  <em style={{ color: '#f85149' }}>-- Odpojit zařízení --</em>
                </MenuItem>
                
                {onlineDevices.map(d => <MenuItem key={d.id} value={d.id}>{d.name}</MenuItem>)}
              </Select>
            </FormControl>

            <Box sx={{ flexGrow: 1 }} />
            
            <Chip
              label={streamStatus}
              color={streamStatus === "Connected" ? "success" : "default"}
              size="small"
              variant="outlined"
            />
          </Toolbar>
        </AppBar>

        {/* MAIN LAYOUT */}
        <Box sx={{ display: 'flex', flexGrow: 1, overflow: 'hidden' }}>

            {/* SLOUPEC: LOGY */}
            <Box sx={{ 
                width: '300px', minWidth: '300px', height: '100%', 
                borderRight: '1px solid #30363d', bgcolor: '#0d1117', 
                display: 'flex', flexDirection: 'column'
            }}>
                <Box sx={{ p: 1, borderBottom: '1px solid #30363d', display: 'flex', alignItems: 'center', bgcolor: '#161b22' }}>
                    <TerminalIcon sx={{ fontSize: 16, color: '#f48fb1', mr: 1 }} />
                    <Typography variant="subtitle2" color="#f48fb1" fontWeight="bold">Logy</Typography>
                </Box>
                <Box sx={{ flexGrow: 1, overflowY: 'auto', p: 1 }}>
                    {liveStream.length === 0 && <Typography variant="caption" sx={{color:'#444', textAlign:'center', mt:2, display:'block'}}>{selectedDeviceId ? "Čekání na data..." : "Nejprve vyberte zařízení"}</Typography>}
                    {liveStream.map((item, index) => (
                        <LogCard key={index} topic={item.topic} payload={item.payload} color="#f48fb1" />
                    ))}
                    <div ref={liveEndRef} />
                </Box>
            </Box>

            {/* SLOUPEC: OVLÁDÁNÍ */}
            <Box sx={{ 
                width: '400px', minWidth: '400px', height: '100%', 
                borderRight: '1px solid #30363d', bgcolor: '#161b22', 
                display: 'flex', flexDirection: 'column'
            }}>
                 <Box sx={{ p: 1, borderBottom: '1px solid #30363d', display: 'flex', alignItems: 'center', bgcolor: '#0d1117' }}>
                    <GamepadIcon sx={{ fontSize: 16, color: '#64b5f6', mr: 1 }} />
                    <Typography variant="subtitle2" color="#64b5f6" fontWeight="bold">Ovládací panel</Typography>
                </Box>
                
                <Box sx={{ flexGrow: 1, overflowY: 'auto', p: 2 }}>
                    <ControlPanel 
                      deviceId={selectedDeviceId} 
                      patientId={selectedPatientId} 
                      onSessionStateChange={setIsSessionActive}
                    />
                </Box>
            </Box>

            {/* SLOUPEC: VIDEO */}
            <Box sx={{ 
                flexGrow: 1, height: '100%', bgcolor: '#000', 
                display: 'flex', flexDirection: 'column', position: 'relative'
            }}>
              <Box sx={{
                  position: 'absolute', top: 0, left: 0, right: 0, zIndex: 10, p: 1,
                  background: 'linear-gradient(to bottom, rgba(0,0,0,0.8) 0%, rgba(0,0,0,0) 100%)',
                  display: 'flex', alignItems: 'center'
                }}>
                  <VideocamIcon sx={{ fontSize: 16, color: '#64b5f6', mr: 1 }} />
                  <Typography variant="subtitle2" sx={{ color: '#fff', mr: 2 }}>Živý přenos</Typography>
                  <Typography variant="caption" sx={{ color: videoStatus === 'STREAMING' ? '#0f0' : '#aaa' }}>
                    {videoStatus}
                  </Typography>
                  
                  {isRecording && <Typography variant="caption" sx={{ color: 'red', ml: 2, fontWeight: 'bold', animation: 'blink 1s infinite' }}>● REC</Typography>}

                  <Box sx={{ flexGrow: 1 }} />
                  
                  <Tooltip title={isRecording ? "Stop Recording" : "Start Recording"}>
                    <IconButton
                      onClick={isRecording ? stopRecording : startRecording}
                      size="small"
                      sx={{ color: isRecording ? '#ff1744' : 'white', mr: 1 }}
                      disabled={videoStatus !== 'STREAMING'}
                    >
                      {isRecording ? <StopIcon /> : <FiberManualRecordIcon />}
                    </IconButton>
                  </Tooltip>

                  <IconButton onClick={toggleFullscreen} size="small" sx={{ color: 'white' }}>
                    {isFullscreen ? <FullscreenExitIcon /> : <FullscreenIcon />}
                  </IconButton>
              </Box>

              <Box 
                ref={videoContainerRef}
                sx={{ 
                    flexGrow: 1, width: '100%', height: '100%', 
                    display: 'flex', justifyContent: 'center', alignItems: 'center',
                    bgcolor: '#000' 
                }}
              >
                <video
                  ref={videoRef}
                  autoPlay
                  playsInline
                  muted
                  style={{ 
                      width: '100%', height: '100%', 
                      objectFit: 'contain' 
                  }}
                />
              </Box>
            </Box>

        </Box>
        
        {/* MODÁLNÍ OKNO PRO NOVÉHO PACIENTA */}
        <Dialog 
          open={isPatientModalOpen} 
          onClose={() => setPatientModalOpen(false)}
          PaperProps={{ style: { backgroundColor: '#161b22', color: '#fff', border: '1px solid #30363d', minWidth: '350px' } }}
        >
          <DialogTitle sx={{ borderBottom: '1px solid #30363d', fontSize: '1rem' }}>Přidat nového pacienta</DialogTitle>
          <DialogContent sx={{ mt: 2, pb: 1 }}>
            <TextField
              autoFocus
              fullWidth
              size="small"
              label="Kód pacienta"
              variant="outlined"
              value={newPatientName}
              onChange={(e) => setNewPatientName(e.target.value)}
              sx={{ 
                input: { color: 'white' }, 
                label: { color: '#8b949e' },
                '& .MuiOutlinedInput-root': {
                  '& fieldset': { borderColor: '#30363d' },
                  '&:hover fieldset': { borderColor: '#58a6ff' },
                }
              }}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault();
                  handleCreatePatient();
                }
              }}
            />
          </DialogContent>
          <DialogActions sx={{ p: 2, borderTop: '1px solid #30363d' }}>
            <Button onClick={() => setPatientModalOpen(false)} sx={{ color: '#8b949e', textTransform: 'none' }}>Zrušit</Button>
            <Button 
              onClick={handleCreatePatient} 
              variant="contained" 
              disabled={!newPatientName.trim() || isCreatingPatient}
              sx={{ bgcolor: '#238636', textTransform: 'none', '&:hover': { bgcolor: '#2ea043' } }}
            >
              {isCreatingPatient ? "Ukládám..." : "Vytvořit"}
            </Button>
          </DialogActions>
        </Dialog>

        <style>{`
          @keyframes blink { 0% { opacity: 1; } 50% { opacity: 0; } 100% { opacity: 1; } }
          ::-webkit-scrollbar { width: 6px; }
          ::-webkit-scrollbar-track { background: #0d1117; }
          ::-webkit-scrollbar-thumb { background: #30363d; border-radius: 3px; }
        `}</style>
      </Box>
    </ThemeProvider>
  );
}

export default App;