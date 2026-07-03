using UnityEngine;
using Newtonsoft.Json;
using App.Models;

public class MainPuzzleManager : MonoBehaviour, IMiniGame
{
    [Header("Prefaby Level Manažerů")]
    public PuzzleLevel1Manager level1Prefab;
    public PuzzleLevel2Manager level2Prefab;
    public PuzzleLevel3Manager level3Prefab;

    // Paměť pro aktuálně běžící level
    private MonoBehaviour currentActiveLevel;

    public void StartGame(string configJson)
    {
        int currentDifficulty = 1;
        int iterations = 1;

        // 1. Získáme iterace
        if (SessionManager.Instance != null)
        {
            iterations = SessionManager.Instance.TotalIterations;
        }

        // 2. Získáme obtížnost z JSONu
        if (!string.IsNullOrEmpty(configJson))
        {
            try
            {
                BaseConfig baseSettings = JsonConvert.DeserializeObject<BaseConfig>(configJson);
                currentDifficulty = baseSettings.difficulty;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MainPuzzleManager] Chyba JSONu, jedu default: {e.Message}");
            }
        }

        Debug.Log($"🧩 MAIN PUZZLE MANAGER: Spouštím Diff {currentDifficulty}, Iterací {iterations}");

        // 3. Smazání předchozího levelu
        if (currentActiveLevel != null)
        {
            Destroy(currentActiveLevel.gameObject);
        }

        // 4. ROZCESTNÍK 
        switch (currentDifficulty)
        {
            case 1:
                if (level1Prefab != null)
                {
                    PuzzleLevel1Manager inst = Instantiate(level1Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                else Debug.LogError("🚨 MainPuzzleManager: Chybí Prefab!");
                break;
            case 2:
                if (level2Prefab != null)
                {
                    PuzzleLevel2Manager inst = Instantiate(level2Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                else Debug.LogError("🚨 MainPuzzleManager: Chybí Prefab!");
                break;
            case 3:
                if (level3Prefab != null)
                {
                    PuzzleLevel3Manager inst = Instantiate(level3Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                else Debug.LogError("🚨 MainPuzzleManager: Chybí Prefab!");
                break;
            default:
                Debug.LogWarning("🚨 Neznámá obtížnost! Spouštím Level 1.");
                if (level1Prefab != null)
                {
                    PuzzleLevel1Manager inst = Instantiate(level1Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                break;
        }
    }

    public void StopGame()
    {
        Debug.Log("🧹 MAIN PUZZLE MANAGER: Úklid hry MrPuzzle.");
        if (currentActiveLevel != null)
        {
            Destroy(currentActiveLevel.gameObject);
            currentActiveLevel = null;
        }
    }
}