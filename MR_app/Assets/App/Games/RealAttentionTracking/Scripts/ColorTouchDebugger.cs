using UnityEngine;
using System.Collections;

public class ColorTouchDebugger : MonoBehaviour
{
    public void BleskniCervene()
    {
        // Spustí bliknutí
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        // Najde všechny vizuální modely v tomto objektu
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        // Uložíme si pùvodní barvy, abychom je mohli vrátit
        Color[] originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else if (renderers[i].material.HasProperty("_BaseColor"))
                originalColors[i] = renderers[i].material.GetColor("_BaseColor");
            else
                originalColors[i] = Color.white;

            // Zmìníme barvu na KØIKLAVÌ ÈERVENOU
            renderers[i].material.color = Color.red;
            if (renderers[i].material.HasProperty("_BaseColor"))
                renderers[i].material.SetColor("_BaseColor", Color.red);
        }

        // Poèkáme pùl sekundy
        yield return new WaitForSeconds(0.5f);

        // Vrátíme pùvodní barvy
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i];
            if (renderers[i].material.HasProperty("_BaseColor"))
                renderers[i].material.SetColor("_BaseColor", originalColors[i]);
        }
    }
}