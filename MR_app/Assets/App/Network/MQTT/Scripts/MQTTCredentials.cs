using UnityEngine;

[CreateAssetMenu(fileName = "MQTTCredentials", menuName = "MQTT/Credentials")]
public class MQTTCredentials : ScriptableObject
{
    public string BrokerAddress = "broker.example.com";
    public int BrokerPort = 8883;                // 8883 for TLS, 1883 for plaintext
    public string Username = "";
    public string Password = "";
    public string TopicPrefix = "/unitymap";              // e.g. "/unitymap"
    public bool UseTls = true;                   // enable/disable TLS
    public bool AllowInsecureCerts = false;      // dev only (self-signed)
}