using UnityEngine;
using Newtonsoft.Json;
using App.Models;


public class MainLocationManager : MonoBehaviour, IMiniGame
{
    [Header("Prefaby Level Manažerů")]
    public LocationLevel1Manager level1Prefab;
    public LocationLevel2Manager level2Prefab;
    public LocationLevel3Manager level3Prefab;

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
                Debug.LogWarning($"🚨 MainLocationManager: Chyba JSONu, jedu default: {e.Message}");
            }
        }

        Debug.Log($"🎮 MAIN LOCATION MANAGER: Vytvářím z Prefabu Level {currentDifficulty} s počtem iterací {iterations}");

        // 3. Smazání předchozího levelu, pokud nějaký běží
        if (currentActiveLevel != null)
        {
            Destroy(currentActiveLevel.gameObject);
        }

        // 4. Podle čísla narve do hry správný Level Prefab
        switch (currentDifficulty)
        {
            case 1:
                if (level1Prefab != null)
                {
                    LocationLevel1Manager inst = Instantiate(level1Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                else Debug.LogError("🚨 MainLocationManager: Nemáš přiřazený Prefab pro Level 1!");
                break;

            case 2:
                if (level2Prefab != null)
                {
                    LocationLevel2Manager inst = Instantiate(level2Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                else Debug.LogError("🚨 MainLocationManager: Nemáš přiřazený Prefab pro Level 2!");
                break;

            case 3:
                if (level3Prefab != null)
                {
                    LocationLevel3Manager inst = Instantiate(level3Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                else Debug.LogError("🚨 MainLocationManager: Nemáš přiřazený Prefab pro Level 3!");
                break;

            default:
                Debug.LogWarning($"🚨 Neznámý Location level {currentDifficulty}! Spouštím Level 1.");
                if (level1Prefab != null)
                {
                    LocationLevel1Manager inst = Instantiate(level1Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                break;
        }
    }

    public void StopGame()
    {
        Debug.Log("🧹 MAIN LOCATION MANAGER: Uklízím hru.");
        if (currentActiveLevel != null)
        {
            Destroy(currentActiveLevel.gameObject);
            currentActiveLevel = null;
        }
    }
}