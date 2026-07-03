import { useState, useEffect, useRef } from 'react';
import { DefaultService } from '../client'; 

export const useWebRTC = (
  videoRef: React.RefObject<HTMLVideoElement | null>,
  deviceId: string,
  incomingSignal: any 
) => {
  const [videoStatus, setVideoStatus] = useState<string>("Waiting for Device...");
  
  // Držíme si referenci na WebRTC spojení, aby nám nezmizelo při překreslení komponenty
  const pcRef = useRef<RTCPeerConnection | null>(null);

  useEffect(() => {
    if (!deviceId) {
      setVideoStatus("Není zvoleno žadné zařízení");
      return;
    }

    setVideoStatus("Connecting...");

    const pc = new RTCPeerConnection();
    pcRef.current = pc;

    pc.ontrack = (event) => {
      console.log("🎥 Video stream zachycen!");
      setVideoStatus("STREAMING");

      if (videoRef.current) {
        let stream = event.streams[0];
        if (!stream) {
          stream = new MediaStream();
          stream.addTrack(event.track);
        }
        videoRef.current.srcObject = stream;
      }
    };

    // B) ODESÍLÁNÍ NAŠICH (LOKÁLNÍCH) ICE KANDIDÁTŮ DO UNITY
    pc.onicecandidate = async (event) => {
      if (event.candidate) {
        try {
          await DefaultService.sendWebrtcToUnityWebrtcSendPost({
            device_id: deviceId,
            payload: {
              type: "candidate",
              candidate: event.candidate.candidate,
              sdpMid: event.candidate.sdpMid,
              sdpMLineIndex: event.candidate.sdpMLineIndex
            } as any
          });
        } catch (e) {
          console.error("❌ Chyba při odesílání ICE kandidáta:", e);
        }
      }
    };

    const wakeUpUnity = async () => {
      try {
        console.log("🔔 Budím Unity, aby poslalo WebRTC Offer...");
        await DefaultService.sendWebrtcToUnityWebrtcSendPost({
          device_id: deviceId,
          payload: { type: "start" } as any 
        });
      } catch (e) {
        console.error("❌ Nepodařilo se poslat budíček do Unity:", e);
      }
    };

    setTimeout(wakeUpUnity, 1500); // Rovnou to zavoláme

    return () => {
      if (pcRef.current) {
        pcRef.current.close();
      }
      if (videoRef.current) {
        videoRef.current.srcObject = null;
      }
    };
  }, [deviceId, videoRef]); // Tento blok se spustí znovu JEN při změně brýlí


  useEffect(() => {
    if (!incomingSignal || !pcRef.current) return;

    const processSignal = async () => {
      const pc = pcRef.current;
      if (!pc) return;

      try {
        
        if (incomingSignal.type === "offer") {
          console.log("📩 Zpracovávám Offer z brýlí...");
          setVideoStatus("Negotiating...");

          // Přijmeme nabídku
          await pc.setRemoteDescription(new RTCSessionDescription(incomingSignal));
          
          // Vytvoříme odpověď (Answer)
          const answer = await pc.createAnswer();
          await pc.setLocalDescription(answer);

          await DefaultService.sendWebrtcToUnityWebrtcSendPost({
            device_id: deviceId,
            payload: { type: "answer", sdp: answer.sdp } as any
          });
          console.log("📤 Answer odeslán do brýlí!");

        } 
        // 2. PŘIŠEL ICE KANDIDÁT (Cesta) Z UNITY
        else if (incomingSignal.type === "candidate" && incomingSignal.candidate) {
          console.log("🔌 Přidávám ICE kandidáta z brýlí...");
          await pc.addIceCandidate(new RTCIceCandidate(incomingSignal));
        }

      } catch (error) {
        console.error("❌ Chyba při zpracování WebRTC signálu z backendu:", error);
      }
    };

    processSignal();
  }, [incomingSignal, deviceId]); 

  return { videoStatus };
};