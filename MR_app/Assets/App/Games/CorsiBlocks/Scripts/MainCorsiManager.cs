using UnityEngine;
using Newtonsoft.Json;
using App.Models;

public class MainCorsiManager : MonoBehaviour, IMiniGame
{
    [Header("Prefaby Level Manažerů")]
    public CorsiLevel1Manager level1Prefab;
    public CorsiLevel2Manager level2Prefab;
    public CorsiLevel3Manager level3Prefab;

    // Paměť pro aktuálně běžící level
    private MonoBehaviour currentActiveLevel;


    public void StartGame(string configJson)
    {
        int currentDifficulty = 1;
        int iterations = 1;

        // 1. Získáme iterace ze SessionManageru
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
                Debug.LogWarning($"🚨 MainCorsiManager: Chyba JSONu, jedu default: {e.Message}");
            }
        }

        Debug.Log($"🎮 MAIN CORSI MANAGER: Vytvářím z Prefabu Level {currentDifficulty} s počtem iterací {iterations}");

        // 3. Smazání předchozího levelu, pokud nějaký běží
        if (currentActiveLevel != null)
        {
            Destroy(currentActiveLevel.gameObject);
        }

        
        switch (currentDifficulty)
        {
            case 1:
                if (level1Prefab != null)
                {
                    CorsiLevel1Manager inst = Instantiate(level1Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                else Debug.LogError("🚨 MainCorsiManager: Nemáš přiřazený Prefab pro Level 1!");
                break;

            case 2:
                if (level2Prefab != null)
                {
                    CorsiLevel2Manager inst = Instantiate(level2Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                else Debug.LogError("🚨 MainCorsiManager: Nemáš přiřazený Prefab pro Level 2!");
                break;

            case 3:
                if (level3Prefab != null)
                {
                    CorsiLevel3Manager inst = Instantiate(level3Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                else Debug.LogError("🚨 MainCorsiManager: Nemáš přiřazený Prefab pro Level 3!");
                break;

            default:
                Debug.LogWarning($"🚨 Neznámý Corsi level {currentDifficulty}! Spouštím Level 1.");
                if (level1Prefab != null)
                {
                    CorsiLevel1Manager inst = Instantiate(level1Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                break;
        }
    }

    
    public void StopGame()
    {
        Debug.Log("🧹 MAIN CORSI MANAGER: Uklízím hru.");
        if (currentActiveLevel != null)
        {
            Destroy(currentActiveLevel.gameObject);
            currentActiveLevel = null;
        }
    }
}