using UnityEngine;
using System.Collections;
using System.Text;
using System.Reflection;
using Meta.XR;

using UnityRTC = Unity.WebRTC;

/// <summary>
/// Zajišťuje streamování MR přes WebRTC. 
/// NYNÍ: Čeká na povel ("start") od Reactu, než začne vysílat Offer!
/// </summary>
public class SimpleQuestStreamer : MonoBehaviour
{
    [Header("MQTT Propojení")]
    [SerializeField] private MQTTPublisher mqttPublisher;
    private string hardwareId;

    [Header("Zdroje")]
    public PassthroughCameraAccess passthroughAccess;
    public Camera streamCamera;

    [Header("Mixážní pult")]
    public Material compositorMaterial;
    public RenderTexture unityObjectsTexture;
    public RenderTexture finalWebRtcTexture;

    private UnityRTC.RTCPeerConnection pc;
    private UnityRTC.VideoStreamTrack videoTrack;
    private bool cameraSynced = false;

    IEnumerator Start()
    {
        hardwareId = DeviceUniqueIdGenerator.GenerateUniqueId();

        if (passthroughAccess != null) yield return new WaitUntil(() => passthroughAccess.IsPlaying);

        try
        {
            var initMethod = typeof(UnityRTC.WebRTC).GetMethod("InitializeInternal", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (initMethod != null) initMethod.Invoke(null, new object[] { true, false, UnityRTC.NativeLoggingSeverity.Info });
        }
        catch { }
        StartCoroutine(UnityRTC.WebRTC.Update());

        yield return new WaitForSeconds(1.0f);
        SetupWebRTC();
    }

    void Update()
    {
        if (passthroughAccess != null && passthroughAccess.IsPlaying && compositorMaterial != null)
        {
            if (!cameraSynced)
            {
                SyncCameraToPassthrough();
            }

            Texture videoTex = passthroughAccess.GetTexture();

            if (videoTex != null)
            {
                compositorMaterial.SetTexture("_ForegroundTex", unityObjectsTexture);
                Graphics.Blit(videoTex, finalWebRtcTexture, compositorMaterial);
            }
        }

        // Manuální spuštění Offeru pro debug ponecháno 
        if (OVRInput.GetDown(OVRInput.Button.One) || Input.GetKeyDown(KeyCode.Space))
            StartCoroutine(CreateAndSendOffer());
    }

    void SyncCameraToPassthrough()
    {
        var intrinsics = passthroughAccess.Intrinsics;
        streamCamera.transform.localRotation = intrinsics.LensOffset.rotation;
        cameraSynced = true;
    }

    private void SetupWebRTC()
    {
        var config = new UnityRTC.RTCConfiguration { iceServers = new[] { new UnityRTC.RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } } };
        pc = new UnityRTC.RTCPeerConnection(ref config);
        videoTrack = new UnityRTC.VideoStreamTrack(finalWebRtcTexture);
        pc.AddTrack(videoTrack);

        pc.OnIceCandidate = c => SendJson(new SignalingMessage { type = "candidate", candidate = c.Candidate, sdpMid = c.SdpMid, sdpMLineIndex = c.SdpMLineIndex ?? 0 });

        
        Debug.Log("📡 WebRTC připraveno. Čekám na povel z Reactu...");
    }

    IEnumerator CreateAndSendOffer()
    {
        Debug.Log("📤 Vytvářím a odesílám Offer...");
        var op = pc.CreateOffer(); yield return op;
        if (!op.IsError)
        {
            var offer = op.Desc;
            yield return pc.SetLocalDescription(ref offer);
            SendJson(new SignalingMessage { type = "offer", sdp = offer.sdp });
        }
    }

    public void HandleIncomingSignaling(string json)
    {
        var msg = JsonUtility.FromJson<SignalingMessage>(json);

        
        if (msg.type == "start")
        {
            Debug.Log("🔔 Budíček! React se chce dívat. Zapínám video...");
            StartCoroutine(CreateAndSendOffer());
        }
        else if (msg.type == "answer")
        {
            Debug.Log("📩 Přijat Answer. Spojuji video!");
            var desc = new UnityRTC.RTCSessionDescription { type = UnityRTC.RTCSdpType.Answer, sdp = msg.sdp };
            pc.SetRemoteDescription(ref desc);
        }
        else if (msg.type == "candidate")
        {
            pc.AddIceCandidate(new UnityRTC.RTCIceCandidate(new UnityRTC.RTCIceCandidateInit { candidate = msg.candidate, sdpMid = msg.sdpMid, sdpMLineIndex = msg.sdpMLineIndex }));
        }
    }

    void SendJson(object data)
    {
        if (mqttPublisher != null)
        {
            
            string topic = $"/{hardwareId}/webrtc";
            string json = JsonUtility.ToJson(data);
            mqttPublisher.SendPayload(topic, json);
        }
    }

    private void OnDestroy()
    {
        videoTrack?.Dispose();
        pc?.Close();
    }

    [System.Serializable] public class SignalingMessage { public string type, sdp, candidate, sdpMid; public int sdpMLineIndex; }
}