using UnityEngine;

public class ItemButton : MonoBehaviour
{
    private int assignedItemIndex;
    private IAttentionLevelManager manager;

    // Manažer to sám zavolá, jakmile tlaèítko vytvoøí
    public void SetupButton(int index, IAttentionLevelManager levelManager)
    {
        assignedItemIndex = index;
        manager = levelManager;
    }

    public void Zmacknuto()
    {
        if (manager != null)
        {
            manager.OnSeniorClickedItem(assignedItemIndex);
        }
    }

    // VR Pojistka pro ovladaèe (Poke event)
    public void OnPoke()
    {
        Zmacknuto();
    }
}