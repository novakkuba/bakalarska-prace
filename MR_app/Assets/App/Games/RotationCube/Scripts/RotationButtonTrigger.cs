using UnityEngine;
using System.Collections;

public class RotationButtonTrigger : MonoBehaviour
{
    public Color successColor = Color.green;
    public Color errorColor = Color.red;
    private Color defaultColor;

    private Renderer rend;

    private IRotationLevelManager manager;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null) defaultColor = rend.material.color;
    }

    // Tuto metodu zavolá manažer při vytvoření Canvasu
    public void SetupButton(IRotationLevelManager levelManager)
    {
        manager = levelManager;
    }

    public void Zmacknuto()
    {
        if (manager != null)
        {
            // Tlačítko pošle manažerovi referenci na SÁM SEBE, aby mu mohl říct výsledek
            manager.CheckAnswerButtonClicked(this);
        }
    }

    // Pojistka pro Meta XR SDK (Poke Interactable)
    public void OnPoke()
    {
        Zmacknuto();
    }

    public void ShowSuccess()
    {
        if (rend != null) rend.material.color = successColor;
    }

    public void ShowErrorAndBlink()
    {
        if (rend != null) StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        rend.material.color = errorColor;
        yield return new WaitForSeconds(0.5f);
        rend.material.color = defaultColor;
    }

    public void ResetColor()
    {
        if (rend != null) rend.material.color = defaultColor;
    }
}