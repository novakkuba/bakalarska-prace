using UnityEngine;

public class MQTTManager : MonoBehaviour
{
    [SerializeField] private MQTTPublisher publisher;
    [SerializeField] private MQTTSubscriber subscriber;

    private string hardwareId;

    private void Start()
    {
        // 1. Získáme unikátní ID zařízení
        hardwareId = DeviceUniqueIdGenerator.GenerateUniqueId();

        publisher.SetClientId(hardwareId);
        subscriber.SetClientId(hardwareId);

        // 2. Nastartujeme připojení k brokeru
        publisher.StartPublisher();
        subscriber.StartSubscriber();

        Debug.Log($"[MQTT Manager] Startuji s ID: {hardwareId}. Odesílám Discovery za 5s.");

        
        Invoke(nameof(SendDiscoveryMessage), 5f);

        
        InvokeRepeating(nameof(SendHeartbeat), 10f, 10f);
    }

    private void SendHeartbeat()
    {
        // Pípání pro backend, že brýle nespí a nevypnuly se
        string json = "{\"status\":\"online\", \"hardware_id\":\"" + hardwareId + "\"}";
        publisher.SendPayload($"/{hardwareId}/status", json);
    }

    private void SendDiscoveryMessage()
    {
        // Vytvoření správného datového balíčku
        DiscoveryPayload payload = new DiscoveryPayload
        {
            hardware_id = hardwareId,
            name = SystemInfo.deviceName 
        };

        // Převod do správného JSON formátu
        string json = JsonUtility.ToJson(payload);

        // Odeslání do Pythonu
        publisher.SendPayload("/discovery", json);

        Debug.Log($"[➔ ODESLÁNO DISCOVERY] Topic: /unitymap/discovery | Payload: {json}");
    }

    [System.Serializable]
    private class DiscoveryPayload
    {
        public string hardware_id;
        public string name;
    }
}