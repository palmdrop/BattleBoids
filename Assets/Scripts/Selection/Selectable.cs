using UnityEngine;

public abstract class Selectable : MonoBehaviour, ISelectable
{
    
    // Adds the selected visualisation 
    public void SetSelectionIndicator(bool isSelected)
    {
        // TODO: now highlight indicator needs to be the second child of the Boid container, should be solved differently
        transform.GetChild(1).gameObject.SetActive(isSelected);
    }
}