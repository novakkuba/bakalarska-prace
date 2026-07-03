import { Dispatch, SetAction } from 'react';

export const useConfigManager = (
  schema: any, 
  setGameType: (type: string) => void, 
  setPayload: Dispatch<any>
) => {

  const handleImport = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const rawResult = e.target?.result as string;
        let importedData = JSON.parse(rawResult);

        // 1. Zvládnutí historie ze ZIPu
        if (Array.isArray(importedData)) {
          console.log("📂 Importuji historii sezení. Vybírám poslední stav.");
          importedData = importedData[importedData.length - 1];
        }

        
        // Použijeme rest operator (...rest), abychom oddělili balast od herních dat
        const { 
          timestamp, 
          action, 
          game_type, 
          game, // občas se to v JSONu může jmenovat jinak
          ...gameParameters 
        } = importedData;

        // 3. Určení typu hry
        const finalGameType = game_type || game || "rotation_cube";

        // 4. Update stavů v ControlPanelu
        setGameType(finalGameType);
        
        // Nastavíme payload - React se postará o to, aby se UI zaktualizovalo
        setPayload(gameParameters);

        console.log("✅ Import hotov. Načteno nastavení pro:", finalGameType);

      } catch (err: any) {
        console.error("❌ Import selhal:", err);
        alert("Chyba při nahrávání: Soubor není platný JSON konfigurace.");
      }
    };

    reader.readAsText(file);
    // Reset inputu, aby šel stejný soubor nahrát znovu (třeba po úpravě)
    event.target.value = ''; 
  };

  return { handleImport };
};