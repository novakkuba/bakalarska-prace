using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using App.Models;

/// <summary>
/// Zpracovává a směruje příchozí zprávy z backendu (přes MQTT) do hlavního vlákna Unity.
/// Funguje jako "Router" - podle názvu topicu pozná, zda jde o herní příkaz, nebo o WebRTC video signalizaci.
/// </summary>
public class MQTTEngine : MonoBehaviour
{
    [Header("Propojení")]
    [SerializeField] private MQTTSubscriber subscriber;
    [SerializeField] private MinigameController minigameController;

    [SerializeField] private SimpleQuestStreamer webrtcStreamer;

    void Start()
    {
        if (subscriber != null)
            subscriber.OnMessageReceived += OnMqttMessageReceived;
    }

    void OnMqttMessageReceived(string topic, string message)
    {
        // Vždy zpracováváme na hlavním vlákně Unity
        if (MainThreadDispatcher.Instance != null)
        {
            MainThreadDispatcher.Instance.Enqueue(() =>
            {
                RouteMessageSafe(topic, message);
            });
        }
    }

    // ROZCESTNÍK 
    void RouteMessageSafe(string topic, string message)
    {
        Debug.Log($"[MQTT ENGINE] Přijata zpráva z topicu: {topic}");

        try
        {
            // Zjistíme, z jaké schránky zpráva vypadla
            if (topic.EndsWith("/command"))
            {
                ProcessGameCommand(message);
            }
            else if (topic.EndsWith("/webrtc_command"))
            {
                Debug.LogWarning($"[🚨 MQTT ENGINE DEBUG 🚨] HALÓ! Trefili jsme webrtc_command! Obsah zprávy: {message}");

                ProcessWebRtcCommand(message);
            }
            else
            {
                Debug.LogWarning($"[MQTT ENGINE] Neznámý topic: {topic}. Zprávu ignoruji.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MQTT ENGINE] Chyba při routování zprávy: {e.Message}");
        }
    }

    void ProcessGameCommand(string message)
    {
        var envelope = JsonConvert.DeserializeObject<MqttEnvelope>(message);

        if (envelope != null && !string.IsNullOrEmpty(envelope.game))
        {
            Debug.Log($"[MQTT ENGINE] Zpracovávám hru: '{envelope.game}' pro Session: {envelope.session_id}");

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.CurrentSessionId = envelope.session_id;
                SessionManager.Instance.CurrentIteration = 1;

                if (envelope.config["iterations"] != null)
                {
                    SessionManager.Instance.TotalIterations = envelope.config["iterations"].Value<int>();
                }
            }

            string configJson = envelope.config.ToString();

            if (minigameController != null)
            {
                minigameController.SwitchGame(envelope.game, configJson);
            }
        }
    }

    void ProcessWebRtcCommand(string message)
    {
        Debug.Log($"[MQTT ENGINE] Přijat WebRTC signál: {message}");

        if (webrtcStreamer != null)
        {
            webrtcStreamer.HandleIncomingSignaling(message);
        }
    }

    void OnDestroy()
    {
        if (subscriber != null)
            subscriber.OnMessageReceived -= OnMqttMessageReceived;
    }
}