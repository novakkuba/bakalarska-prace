using UnityEngine;
using System.Collections;


public class CorsiBlock : MonoBehaviour
{
    [HideInInspector]
    public int ID;

    private Renderer rend;
    private Color originalColor;
    private Coroutine highlightCoroutine;

    
    private ICorsiLevelManager manager;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color;
        }
    }

    
    public void SetupBlock(ICorsiLevelManager levelManager, int blockID)
    {
        manager = levelManager;
        ID = blockID;
    }

    public void Highlight(Color color, float duration)
    {
        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
        }
        highlightCoroutine = StartCoroutine(HighlightRoutine(color, duration));
    }

    private IEnumerator HighlightRoutine(Color color, float duration)
    {
        if (rend != null) rend.material.color = color;
        yield return new WaitForSeconds(duration);
        if (rend != null) rend.material.color = originalColor;
    }

    public void Zmacknuto()
    {
        // Rychlý vizuální feedback po kliknutí
        Highlight(Color.cyan, 0.15f);

        
        if (manager != null)
        {
            manager.PlayerSelectedBlock(ID);
        }
    }

    // Pojistka pro Meta XR SDK (Poke Interactable)
    public void OnPoke()
    {
        Zmacknuto();
    }
}