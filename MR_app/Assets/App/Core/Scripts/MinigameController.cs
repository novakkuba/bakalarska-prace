using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;  
using App.Models;       

/// <summary>
/// Centrální orchestrátor pro správu životního cyklu miniher.
/// Zajišťuje dynamické přepínání mezi herními scénáři na základě příkazů z backendu, stará se o bezpečné čištění paměti při změně hry a distribuci konfiguračních parametrů (obtížnost, počet iterací) do jednotlivých modulů.
/// </summary>

public class MinigameController : MonoBehaviour
{
    [SerializeField] private MQTTPublisher mqttPublisher;

    [System.Serializable]
    public struct GameDefinition
    {
        public string gameId;
        public GameObject prefab;
    }

    [Header("Nastaven� Her")]
    public List<GameDefinition> availableGames;

    private IMiniGame currentActiveGame;
    private GameObject currentGameObject;

    public void SwitchGame(string gameId, string configJson)
    {
        Debug.Log($"[MinigameController] P�ijat p��kaz pro: '{gameId}'");

        // 1. UKLIDIT STAROU HRU
        if (currentGameObject != null)
        {
            currentActiveGame?.StopGame();
            currentGameObject.SetActive(false); 
            Destroy(currentGameObject);         

            currentActiveGame = null;
            currentGameObject = null;
        }

        if (gameId == "reset") return;
        
        GameDefinition selectedGame = availableGames.Find(g => g.gameId == gameId);

        if (selectedGame.prefab == null)
        {
            Debug.LogError($"[MinigameController] Hra '{gameId}' nenalezena v seznamu!");
            return;
        }

        int currentDifficulty = 1; 

        if (!string.IsNullOrEmpty(configJson))
        {
            try
            {
                BaseConfig baseSettings = JsonConvert.DeserializeObject<BaseConfig>(configJson);

                currentDifficulty = baseSettings.difficulty;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MinigameController] Nelze na��st BaseConfig (pou��v�m default): {e.Message}");
            }
        }

        currentGameObject = Instantiate(selectedGame.prefab, this.transform);
        currentActiveGame = currentGameObject.GetComponent<IMiniGame>();

        if (currentActiveGame != null)
        {
            
            currentActiveGame.StartGame(configJson);

            

            Debug.Log($"[MinigameController] Hra '{gameId}' b��. (Diff: {currentDifficulty})");
        }
        else
        {
            Debug.LogError($"[MinigameController] Prefab '{selectedGame.prefab.name}' nem� IMiniGame!");
        }
    }
}