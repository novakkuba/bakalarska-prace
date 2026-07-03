using UnityEngine;

public class AutoFitBoxCollider : MonoBehaviour
{
    // Toto magické tlaèítko se ti objeví v Unity v Inspectoru!
    [ContextMenu("Opravit Collider (Automaticky)")]
    public void FitCollider()
    {
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc == null)
        {
            bc = gameObject.AddComponent<BoxCollider>();
        }

        // 1. Doèasnì vyresetujeme rotaci, aby se krabice nezkøivila
        Quaternion oldRot = transform.rotation;
        transform.rotation = Quaternion.identity;

        // 2. Najdeme všechny vizuální 3D modely, které v tomto objektu a jeho dìtech jsou
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"Objekt {gameObject.name} nemá žádný vizuální model!");
            transform.rotation = oldRot;
            return;
        }

        // 3. Obalíme to všechno neviditelnou krabicí
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        // 4. Nastavíme pøesný støed
        bc.center = transform.InverseTransformPoint(bounds.center);

        // 5. Nastavíme pøesnou velikost a vyrušíme jakýkoliv podivný Scale!
        Vector3 localSize = bounds.size;

        if (transform.lossyScale.x != 0) localSize.x /= transform.lossyScale.x;
        if (transform.lossyScale.y != 0) localSize.y /= transform.lossyScale.y;
        if (transform.lossyScale.z != 0) localSize.z /= transform.lossyScale.z;

        // Pøidáme 10 % objemu navíc (rezerva pro snadnìjší dotyk ve VR)
        bc.size = localSize * 1.1f;

        // 6. Vrátíme pùvodní rotaci
        transform.rotation = oldRot;

        Debug.Log($"<color=green>HOTOVO:</color> Collider pro {gameObject.name} byl perfektnì obalen!");
    }
}