using UnityEngine;

public class FixMeshLayer : MonoBehaviour
{
    // Tuhle funkci zavoláme, jakmile Meta dostaví zdi
    public void FixLayer(MeshFilter generatedMesh)
    {
        // Pøevezmeme vrstvu z rodièe (Environment) a vnutíme ji novým zdem
        generatedMesh.gameObject.layer = gameObject.layer;
        Debug.Log($"VRSTVA OPRAVENA: Zdi byly pøesunuty do vrstvy: {LayerMask.LayerToName(gameObject.layer)}");
    }
}