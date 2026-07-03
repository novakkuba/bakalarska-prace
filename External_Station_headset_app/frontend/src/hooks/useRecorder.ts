import { useState, useRef } from 'react';

export const useRecorder = (videoRef: React.RefObject<HTMLVideoElement | null>) => {
  const [isRecording, setIsRecording] = useState(false);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<Blob[]>([]);

  const startRecording = () => {
    const stream = videoRef.current?.srcObject as MediaStream;
    if (!stream) {
      alert("Není k dispozici žádný video stream k nahrávání!");
      return;
    }

    try {
      const mediaRecorder = new MediaRecorder(stream, { mimeType: 'video/webm' });
      mediaRecorderRef.current = mediaRecorder;
      chunksRef.current = [];

      mediaRecorder.ondataavailable = (event) => {
        if (event.data && event.data.size > 0) {
          chunksRef.current.push(event.data);
        }
      };

      mediaRecorder.onstop = () => {
        const blob = new Blob(chunksRef.current, { type: 'video/webm' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        a.download = `zaznam-unity-${timestamp}.webm`;
        a.click();
        window.URL.revokeObjectURL(url);
      };

      mediaRecorder.start();
      setIsRecording(true);
      console.log("🎥 Nahrávání spuštěno...");
    } catch (e) {
      console.error("Chyba při spouštění nahrávání:", e);
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
      setIsRecording(false);
      console.log("🎥 Zastavuji nahrávání...");
    }
  };

  return { isRecording, startRecording, stopRecording };
};