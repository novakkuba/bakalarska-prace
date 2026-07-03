using UnityEngine;
using System.Collections.Generic;

public class RotationCube : MonoBehaviour
{
    [Header("Nastavení barev (Zadej přesně 6!)")]
    public MeshRenderer cubeRenderer;
    public Material[] baseMaterials;

    [Header("Diagnostika")]
    public bool disableShuffling = true;

    private Dictionary<Material, Vector3> materialToDirectionMap = new Dictionary<Material, Vector3>();
    private Material[] activeMaterials;

    // Přímá reference na manažer pro telemetrii úchopu
    private IRotationLevelManager manager;

    private Vector3[] localAxes = new Vector3[6]
    {
        Vector3.down,     
        Vector3.up,       
        Vector3.back,     
        Vector3.left,     
        Vector3.right,    
        Vector3.forward   
    };

    public void SetupCube(IRotationLevelManager levelManager)
    {
        manager = levelManager;
    }

    // --- TELEMETRIE ÚCHOPŮ ---
    public void OnGrab()
    {
        if (manager != null) manager.LogEvent("Cube_Grab", "RotationCube");
    }

    public void OnRelease()
    {
        if (manager != null) manager.LogEvent("Cube_Release", "RotationCube");
    }

    public void InitializeRandomColors()
    {
        materialToDirectionMap.Clear();

        if (baseMaterials.Length != 6)
        {
            Debug.LogError("🚨 CHYBA: Nemáš v Base Materials 6 barev!");
            return;
        }

        List<Material> materialsToApply = new List<Material>(baseMaterials);

        if (!disableShuffling)
        {
            for (int i = 0; i < materialsToApply.Count; i++)
            {
                Material temp = materialsToApply[i];
                int randomIndex = Random.Range(i, materialsToApply.Count);
                materialsToApply[i] = materialsToApply[randomIndex];
                materialsToApply[randomIndex] = temp;
            }
        }

        cubeRenderer.materials = materialsToApply.ToArray();
        activeMaterials = cubeRenderer.materials;

        if (activeMaterials.Length != 6) return;

        for (int i = 0; i < 6; i++)
        {
            materialToDirectionMap.Add(activeMaterials[i], localAxes[i]);
        }
    }

    public Material GetRandomTargetMaterial()
    {
        List<Material> validMaterials = new List<Material>();
        foreach (var kvp in materialToDirectionMap)
        {
            Vector3 worldDir = transform.TransformDirection(kvp.Value);
            if (Vector3.Dot(worldDir, Vector3.up) < 0.5f)
            {
                validMaterials.Add(kvp.Key);
            }
        }

        if (validMaterials.Count == 0) return activeMaterials[Random.Range(0, 6)];
        return validMaterials[Random.Range(0, validMaterials.Count)];
    }

    public Vector3 GetWorldDirectionOfMaterial(Material targetMat)
    {
        if (materialToDirectionMap.ContainsKey(targetMat))
        {
            return transform.TransformDirection(materialToDirectionMap[targetMat]);
        }
        return Vector3.zero;
    }

    public void HighlightCorrect(bool success)
    {
        // Ponecháno pro tvou vizuální logiku
    }
}