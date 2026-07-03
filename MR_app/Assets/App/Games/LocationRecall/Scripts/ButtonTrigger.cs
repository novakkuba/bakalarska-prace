using UnityEngine;

public class ButtonTrigger : MonoBehaviour
{
    [Header("Nastavení vzhledu")]
    public Color successColor = Color.green;

    private bool alreadyPressed = false;
    private ILocationLevelManager manager;

    // Tuto metodu zavolá manažer ihned po Instantiate
    public void SetupButton(ILocationLevelManager levelManager)
    {
        manager = levelManager;
        alreadyPressed = false;
    }

    public void Zmacknuto()
    {
        if (alreadyPressed || manager == null) return;

        alreadyPressed = true;
        manager.OnConfirmPlacement();
        ZmenBarvu();
    }

    // VR Pojistka
    public void OnPoke()
    {
        Zmacknuto();
    }

    private void ZmenBarvu()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = successColor;
        }
    }
}