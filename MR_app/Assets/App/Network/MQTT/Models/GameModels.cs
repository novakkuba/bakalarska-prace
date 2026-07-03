using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.Models
{
    /// <summary>
    /// Datové modely (C# protějšek Python Pydantic schémat) pro příjem nastavení od lékaře.
    /// Obsahují specifické parametry pro jednotlivé hry a hlavní MQTT obálku pro dynamickou deserializaci (pomocí Newtonsoft JToken).
    /// </summary>
    
    // --- Z�KLADN� T��DA (BaseGameSettings) ---
    [Serializable]
    public class BaseConfig
    {
        public int difficulty;
        public int iterations; // Backend pos�l� celkov� po�et
    }

    // --- DEFINICE JEDNOTLIV�CH HER ---

    [Serializable]
    public class RotationCubeConfig : BaseConfig
    {
        public float speed;
    }

    [Serializable]
    public class CorsiBlocksConfig : BaseConfig
    {
        public int block_count;
    }

    [Serializable]
    public class LocationRecallConfig : BaseConfig
    {
        public int item_count;
    }

    [Serializable]
    public class AttentionTrackingConfig : BaseConfig
    {
        public float target_speed;
    }

    [Serializable]
    public class MrPuzzleConfig : BaseConfig
    {
        public int piece_count;
    }

    // --- OB�LKA (To, co re�ln� p�ijde p�es MQTT) ---
    // Mus� odpov�dat tomu, co pos�l� Python Controller
    [Serializable]
    public class MqttEnvelope
    {
        public int session_id;
        public string game; // Toto pou�ijeme jako diskrimin�tor
        public JToken config; // Newtonsoft ponech� config jako "surov�" objekt k dal��mu zpracov�n�
    }
}