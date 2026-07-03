using System;
using UnityEngine;

/// <summary>
/// Datový model pro serializaci pozice a rotace VR kamery.
/// POZNÁMKA: V současné verzi aplikace se aktivně nepoužívá, ponecháno pro případné budoucí rozšíření
/// </summary>

[Serializable]
public class CameraPositionMessage
{
    public string id;
    public Vector3 position;
    public Quaternion rotation;

    public CameraPositionMessage(string id, Vector3 position, Quaternion rotation)
    {
        this.id = id;
        this.position = position;
        this.rotation = rotation;
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static CameraPositionMessage FromJson(string json)
    {
        return JsonUtility.FromJson<CameraPositionMessage>(json);
    }
}