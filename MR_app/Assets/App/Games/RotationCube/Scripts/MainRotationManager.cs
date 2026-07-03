using UnityEngine;
using Newtonsoft.Json;
using App.Models;

public class MainRotationManager : MonoBehaviour, IMiniGame
{
    [Header("Prefaby Level Manažerů")]
    public RotationLevel1Manager level1Prefab;
    public RotationLevel2Manager level2Prefab;
    public RotationLevel3Manager level3Prefab;

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
                Debug.LogWarning($"🚨 MainRotationManager: Chyba JSONu, jedu default: {e.Message}");
            }
        }

        Debug.Log($"🎮 MAIN ROTATION MANAGER: Vytvářím z Prefabu Level {currentDifficulty} s počtem iterací {iterations}");

        // 3. Pokud už nějaký level běží, pro jistotu ho smažeme
        if (currentActiveLevel != null)
        {
            Destroy(currentActiveLevel.gameObject);
        }

        // Vytvoříme instanci správného Prefabu a odstartujeme ho
        switch (currentDifficulty)
        {
            case 1:
                if (level1Prefab != null)
                {
                    RotationLevel1Manager inst = Instantiate(level1Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                else Debug.LogError("🚨 MainRotationManager: Nemáš přiřazený Prefab pro Level 1!");
                break;

            case 2:
                if (level2Prefab != null)
                {
                    RotationLevel2Manager inst = Instantiate(level2Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                else Debug.LogError("🚨 MainRotationManager: Nemáš přiřazený Prefab pro Level 2!");
                break;

            case 3:
                if (level3Prefab != null)
                {
                    RotationLevel3Manager inst = Instantiate(level3Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                else Debug.LogError("🚨 MainRotationManager: Nemáš přiřazený Prefab pro Level 3!");
                break;

            default:
                Debug.LogWarning($"🚨 Neznámý Rotation level {currentDifficulty}! Spouštím Level 1.");
                if (level1Prefab != null)
                {
                    RotationLevel1Manager inst = Instantiate(level1Prefab, transform);
                    currentActiveLevel = inst;
                    inst.StartLevel(iterations);
                }
                break;
        }
    }

    public void StopGame()
    {
        Debug.Log("🧹 MAIN ROTATION MANAGER: Uklízím hru.");
        if (currentActiveLevel != null)
        {
            Destroy(currentActiveLevel.gameObject);
            currentActiveLevel = null;
        }
    }
}