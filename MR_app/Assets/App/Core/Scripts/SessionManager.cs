using UnityEngine;
using Newtonsoft.Json;
using App.Models;

/// <summary>
/// Centrální mozek herního sezení, který spravuje globální stav (ID sezení, aktuální kolo).
/// Automaticky získává Hardware ID a směruje veškerou herní telemetrii na dynamický MQTT topic.
/// </summary>
public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    private string hardwareId;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            hardwareId = DeviceUniqueIdGenerator.GenerateUniqueId();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Session State")]
    public int CurrentSessionId = 0;
    public int CurrentIteration = 1;
    public int TotalIterations = 1;

    [Header("Dependencies")]
    [SerializeField] private MQTTPublisher mqttPublisher;

    
    public void SendLog<T>(T rawData)
    {
        if (mqttPublisher == null)
        {
            Debug.LogError("[SessionManager] Chybí reference na MQTTPublisher!");
            return;
        }

        // 1. Vytvoříme ten absolutně přesný topic
        string actualMqttTopic = $"/{hardwareId}/logs";

        var envelope = new GameMessage<T>(
            CurrentSessionId,
            CurrentIteration,
            actualMqttTopic,
            rawData
        );

        // 3. Převedeme a pošleme
        string jsonPayload = JsonConvert.SerializeObject(envelope);
        mqttPublisher.SendPayload(actualMqttTopic, jsonPayload);
    }

    public void NextIteration()
    {
        if (CurrentIteration < TotalIterations)
        {
            CurrentIteration++;
            Debug.Log($"[SESSION] Přechod na iteraci {CurrentIteration}/{TotalIterations}");
        }
        else
        {
            Debug.Log("[SESSION] Všechny iterace dokončeny.");
        }
    }

    public void SendSystemStatus(string status, string gameName, int difficulty, int iterations)
    {
        var statusPayload = new
        {
            status = status,
            game = gameName,
            difficulty = difficulty,
            iterations = iterations
        };

        SendLog(statusPayload);
    }
}