using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Zajišťuje asynchronní příjem dat přes MQTT.
/// Nyní poslouchá POUZE na přesných, dedikovaných topicech pro tento konkrétní headset.
/// </summary>
public class MQTTSubscriber : MonoBehaviour
{
    [SerializeField] private MQTTCredentials credentials;

    private IMqttClient client;
    private MqttClientOptions options;
    private readonly List<string> queue = new List<string>();
    private CancellationTokenSource cts;
    private bool isConnecting;
    private bool isDisconnecting;

    public event Action<string, string> OnMessageReceived;

    private string clientId; // Tady je náš čistý hash (např. a1b2c3d4)

    public void StartSubscriber()
    {
        StartSubscribing();
    }

    private async void StartSubscribing()
    {
        if (!credentials)
        { Debug.LogError("MQTT Credentials not assigned!"); return; }
        cts = new CancellationTokenSource();
        await ConnectAndSubscribeAsync(cts.Token);
    }

    public void SetClientId(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("Client ID cannot be null or empty!");
            return;
        }
        clientId = id;
    }

    MqttClientOptions BuildOptions()
    {
        var b = new MqttClientOptionsBuilder()
            .WithClientId(clientId + "_Sub") // Broker nás vidí jako "a1b2_Sub"
            .WithTcpServer(credentials.BrokerAddress, credentials.BrokerPort)
            .WithCredentials(credentials.Username, credentials.Password)
            .WithCleanSession()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
            .WithTimeout(TimeSpan.FromSeconds(15));

        if (credentials.UseTls)
        {
            b = b.WithTlsOptions(o =>
            {
                o.UseTls();
                o.WithSslProtocols(SslProtocols.Tls12);
                if (credentials.AllowInsecureCerts)
                    o.WithCertificateValidationHandler(_ => true);
            });
        }
        return b.Build();
    }

    async Task ConnectAndSubscribeAsync(CancellationToken token)
    {
        if (isConnecting) return;
        isConnecting = true;

        try
        {
            client ??= new MqttFactory().CreateMqttClient();

            // PŘÍJEM ZPRÁVY
            client.ApplicationMessageReceivedAsync += e =>
            {
                var topic = e.ApplicationMessage.Topic ?? "";
                var payload = e.ApplicationMessage.PayloadSegment.Array != null
                    ? Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)
                    : "";

                Debug.Log($"[MQTT Subscriber] Přijato na {topic}: {payload}");

                // UŽ ŽÁDNÉ FILTROVÁNÍ! Co sem přijde, je na 100 % pro nás.
                lock (queue) queue.Add($"{topic}|{payload}");
                OnMessageReceived?.Invoke(topic, payload);
                return Task.CompletedTask;
            };

            client.DisconnectedAsync += async e =>
            {
                if (token.IsCancellationRequested || isDisconnecting) return;

                if (e.Reason != MqttClientDisconnectReason.NormalDisconnection)
                    Debug.LogWarning($"MQTT unexpected disconnection: {e.Reason}");

                await Task.Delay(5000, token);
                await ReconnectWithBackoff(token);
            };

            options = BuildOptions();
            await client.ConnectAsync(options, token);

            // 🎯 PŘIHLÁŠENÍ K ODBĚRU (SUBSCRIPTION) NA PŘESNÉ TOPICY
            await SubscribeToTopics(token);

            Debug.Log($"[MQTT Subscriber] Connected & subscribed to: /unitymap/{clientId}/*");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Connect/Subscribe error: {ex.Message}");
            _ = ReconnectWithBackoff(token);
        }
        finally { isConnecting = false; }
    }

    async Task ReconnectWithBackoff(CancellationToken token)
    {
        var delay = 5000;
        var maxAttempts = 5;
        var attempts = 0;

        while (!token.IsCancellationRequested && (client == null || !client.IsConnected) && attempts < maxAttempts)
        {
            attempts++;
            try
            {
                await Task.Delay(delay, token);
                if (token.IsCancellationRequested) return;

                await client.ConnectAsync(options ?? BuildOptions(), token);

                // 🎯 PO RECONNECTU SE ZNOVU PŘIHLÁSÍME NA TY SAMÉ TOPICY
                await SubscribeToTopics(token);

                Debug.Log($"Reconnected (subscriber) after {attempts} attempts.");
                return;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Reconnect attempt {attempts} failed: {ex.Message}");
                delay = Math.Min(delay * 2, 30000);
            }
        }
    }

    // Pomocná metoda pro přihlášení k odběru (abychom to nepsali dvakrát)
    private async Task SubscribeToTopics(CancellationToken token)
    {
        // 1. Topic pro herní povely (start, pauza...)
        var commandFilter = new MqttTopicFilterBuilder()
            .WithTopic($"/unitymap/{clientId}/command/#")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        // 2. Topic pro WebRTC signaling (video spojení)
        var webrtcFilter = new MqttTopicFilterBuilder()
            .WithTopic($"/unitymap/{clientId}/webrtc_command/#")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        await client.SubscribeAsync(commandFilter, token);
        await client.SubscribeAsync(webrtcFilter, token);
    }

    void Update()
    {
        lock (queue) queue.Clear(); // případné zpracování na main threadu
    }

    async void OnDestroy()
    {
        isDisconnecting = true;
        try
        {
            cts?.Cancel();
            if (client?.IsConnected == true)
                await client.DisconnectAsync(MqttClientDisconnectOptionsReason.NormalDisconnection);
        }
        catch { }
        finally { cts?.Dispose(); }
    }

    internal IMqttClient GetClient() => client;
}