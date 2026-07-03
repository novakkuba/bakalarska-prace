using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Security.Authentication;

/// <summary>
/// Zajišťuje asynchronní odesílání dat (telemetrie, herních logů) z Unity do MQTT brokeru.
/// Obsahuje robustní správu síťového připojení, včetně bezpečného odesílání a automatického znovupřipojení (exponential backoff) při výpadku sítě.
/// </summary>

public class MQTTPublisher : MonoBehaviour
{
    [SerializeField] private MQTTCredentials credentials;

    private IMqttClient client;
    private MqttClientOptions options;
    private CancellationTokenSource cts;
    private readonly object connectionLock = new object();
    private bool isConnecting = false;

    public event Action<string, string> OnMessagePublished;

    private string clientId;


    public void StartPublisher()
    {
        StartPublishing();
    }

    private async void StartPublishing()
    {
        if (!credentials)
        { Debug.LogError("MQTT Credentials not assigned!"); return; }
        cts = new CancellationTokenSource();
        await ConnectAsync(cts.Token);
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
            .WithClientId(clientId + "_Pub")
            .WithTcpServer(credentials.BrokerAddress, credentials.BrokerPort)
            .WithCredentials(credentials.Username, credentials.Password)
            .WithCleanSession()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
            .WithTimeout(TimeSpan.FromSeconds(10));

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

    async Task ConnectAsync(CancellationToken token)
    {
        lock (connectionLock)
        {
            if (isConnecting) return;
            isConnecting = true;
        }

        try
        {
            client ??= new MqttFactory().CreateMqttClient();
            client.DisconnectedAsync += async e =>
            {
                if (token.IsCancellationRequested)
                    return;
                Debug.LogWarning($"Publisher disconnected: {e.Reason}");
                lock (connectionLock)
                {
                    isConnecting = false;
                }
                await ReconnectWithBackoff(token);
            };
            options = BuildOptions();
            await client.ConnectAsync(options, token);
            Debug.Log("Connected (publisher).");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Publisher connect error: {ex.Message}");
            _ = ReconnectWithBackoff(token);
        }
        finally
        {
            lock (connectionLock)
            {
                isConnecting = false;
            }
        }
    }

    async Task ReconnectWithBackoff(CancellationToken token)
    {
        lock (connectionLock)
        {
            if (isConnecting) return;
            isConnecting = true;
        }

        var delay = 2000;
        while (!token.IsCancellationRequested && (client == null || !client.IsConnected))
        {
            try
            {
                await Task.Delay(delay, token);
                await client.ConnectAsync(options ?? BuildOptions(), token);
                Debug.Log("Reconnected (publisher).");
                return;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Reconnect failed: {ex.Message}");
                delay = Math.Min((int)(delay * 1.7f), 30000);
            }
        }

        lock (connectionLock)
        {
            isConnecting = false;
        }
    }

    public async void SendPayload(string topic, string payload)
    {
        // 1. Safety Check: Are we connected?
        if (client == null || !client.IsConnected)
        {
            Debug.LogWarning($"[MQTTPublisher] ⚠️ Client disconnected. Reconnecting before sending to '{topic}'...");
            await ConnectAsync(cts?.Token ?? CancellationToken.None);

            if (client == null || !client.IsConnected)
            {
                Debug.LogError("[MQTTPublisher] ❌ FAILED. Could not reconnect.");
                return;
            }
        }

        // 2. Combine Prefix + Topic (e.g. "/unitymap" + "/headset/detections")
        var fullTopic = string.IsNullOrEmpty(credentials.TopicPrefix)
            ? topic
            : $"{credentials.TopicPrefix}{(topic.StartsWith("/") ? "" : "/")}{topic}";

        // 3. Build the Message
        var msg = new MqttApplicationMessageBuilder()
            .WithTopic(fullTopic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce) // Standard QoS 0
            .WithRetainFlag(false) // Standard non-sticky message
            .Build();

        // 4. Send and Log
        try
        {
            // This is the heavy lifting. We wait for it to finish.
            await client.PublishAsync(msg);

            // 🚨 THIS IS THE LOG YOU WANT 🚨
            Debug.Log($"[MQTTPublisher] ✅ SENT Message to: '{fullTopic}'");

            OnMessagePublished?.Invoke(fullTopic, payload);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MQTTPublisher] 💥 Error during Publish: {e.Message}");
        }
    }

    async void OnDestroy()
    {
        try
        { cts?.Cancel(); if (client?.IsConnected == true) await client.DisconnectAsync(); }
        catch { }
        finally { cts?.Dispose(); }
    }

    internal IMqttClient GetClient() => client;
}
