using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    [Header("Nastavení Přichytávání")]
    public float snapDistance = 0.06f;

    // NOVÉ: Přepínač, zda je tento dílek návnada
    [HideInInspector]
    public bool isDistractor = false;

    private Vector3 correctPosition;
    private Quaternion correctRotation;
    private bool isLocked = false;

    private IPuzzleLevelManager manager;

    public void SetupPiece(IPuzzleLevelManager levelManager)
    {
        manager = levelManager;
        correctPosition = transform.position;
        correctRotation = transform.rotation;
    }

    public void OnGrab()
    {
        if (isLocked) return;
        // TELEMETRIE: Rozlišení pravého dílku a distraktoru
        if (manager != null) manager.LogEvent(isDistractor ? "Distractor_Grab" : "Grab", gameObject.name);
    }

    public void OnRelease()
    {
        if (isLocked) return;
        if (manager != null) manager.LogEvent(isDistractor ? "Distractor_Release" : "Release", gameObject.name);

        // Falešný dílek se nikdy ani nepokusí přicvaknout k desce
        if (!isDistractor)
        {
            CheckSnap();
        }
    }

    public void CheckSnap()
    {
        if (isLocked) return;

        float dist = Vector3.Distance(transform.position, correctPosition);
        if (dist <= snapDistance)
        {
            SnapIt();
        }
        else
        {
            if (manager != null) manager.LogEvent("Drop_or_Miss", gameObject.name);
        }
    }

    void SnapIt()
    {
        isLocked = true;
        if (manager != null) manager.LogEvent("Snap_Success", gameObject.name);

        if (GetComponent<Rigidbody>()) Destroy(GetComponent<Rigidbody>());

        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders) Destroy(col);

        Component[] allComponents = GetComponentsInChildren<Component>();
        foreach (Component c in allComponents)
        {
            string name = c.GetType().Name;
            if (name == "Grabbable" || name == "GrabInteractable" || name == "HandGrabInteractable")
            {
                Destroy(c);
            }
        }

        transform.position = correctPosition;
        transform.rotation = correctRotation;
        transform.localScale = transform.localScale * 1.02f;

        if (manager != null)
        {
            manager.PiecePlaced();
        }
    }
}