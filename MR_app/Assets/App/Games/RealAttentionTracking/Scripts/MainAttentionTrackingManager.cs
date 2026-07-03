using UnityEngine;
using Newtonsoft.Json;
using App.Models;

/// <summary>
/// Hlavní přepínač pro hru Attention Tracking.
/// Spustí jeden univerzální manažer a předá mu, jaký level se má hrát.
/// </summary>
public class MainAttentionTrackingManager : MonoBehaviour, IMiniGame
{
    [Header("Univerzální Level Manažer")]
    public AttentionTrackingManager unifiedManagerPrefab; // Používáme už jen jeden prefab!

    // Paměť pro aktuálně běžící minihru
    private AttentionTrackingManager currentActiveManager;

    // --- VSTUPNÍ BRÁNA Z TABLETU ---
    public void StartGame(string configJson)
    {
        int currentDifficulty = 1;
        int iterations = 1;

        // 1. Získáme iterace
        if (SessionManager.Instance != null)
        {
            iterations = SessionManager.Instance.TotalIterations;
        }

        // 2. Získáme obtížnost
        if (!string.IsNullOrEmpty(configJson))
        {
            try
            {
                BaseConfig baseSettings = JsonConvert.DeserializeObject<BaseConfig>(configJson);
                currentDifficulty = baseSettings.difficulty;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"🚨 MainAttentionTrackingManager: Chyba JSONu, jedu default: {e.Message}");
            }
        }

        Debug.Log($"🎮 MAIN ATTENTION MANAGER: Spouštím Attention Tracking pro Level {currentDifficulty} s {iterations} koly.");

        // 3. Smazání předchozí hry, abychom měli čistý stůl
        if (currentActiveManager != null)
        {
            Destroy(currentActiveManager.gameObject);
        }

        // 4. Spuštění univerzálního manažera
        if (unifiedManagerPrefab != null)
        {
            // Vytvoříme instanci našeho chytrého motoru
            currentActiveManager = Instantiate(unifiedManagerPrefab, transform);

            // Tady posíláš rovnou OBOJÍ do jednoho skriptu, přesně jak to máš vymyšlené
            currentActiveManager.StartLevel(currentDifficulty, iterations);
        }
        else
        {
            Debug.LogError("🚨 MainAttentionTrackingManager: Nemáš přiřazený Prefab pro Univerzálního Manažera!");
        }
    }

    // --- VÝSTUPNÍ BRÁNA ---
    public void StopGame()
    {
        Debug.Log("🧹 MAIN ATTENTION MANAGER: Uklízím hru.");
        if (currentActiveManager != null)
        {
            Destroy(currentActiveManager.gameObject);
            currentActiveManager = null;
        }
    }
}