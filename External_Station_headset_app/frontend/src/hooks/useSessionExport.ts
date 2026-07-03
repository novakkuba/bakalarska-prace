import { useState } from 'react';
import { BACKEND_URL } from '../config';

export const useSessionExport = () => {
  const [isExporting, setIsExporting] = useState(false);

  const downloadSessionZip = async (sessionId: number, fullQueueConfig: any[]) => {
    setIsExporting(true);
    try {
      const response = await fetch(`${BACKEND_URL}/api/export/${sessionId}/zip`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(fullQueueConfig)
      });

      if (!response.ok) throw new Error("Chyba exportu na backendu.");

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `session_${sessionId}_export.zip`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);

      return true; // Úspěch

    } catch (error) {
      console.error("Export selhal:", error);
      alert("❌ Nepodařilo se stáhnout data.");
      return false;
    } finally {
      setIsExporting(false);
    }
  };

  return { downloadSessionZip, isExporting };
};