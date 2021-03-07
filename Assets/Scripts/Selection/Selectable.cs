using System;
using System.ComponentModel;
using UnityEngine;

public abstract class Selectable : MonoBehaviour, ISelectable
{
    [SerializeField] private GameObject selectionIndicatorPrefab;
    private GameObject selectionIndicator;
    private bool _selected = false;

    private void Awake()
    {
       selectionIndicator = Instantiate(selectionIndicatorPrefab, Vector3.zero, Quaternion.identity);
       selectionIndicator.transform.parent = transform;
    }

    // Adds the selected visualisation 
    public void SetSelectionIndicator(bool isSelected)
    {
        // TODO: now highlight indicator needs to be the second child of the Boid container, should be solved differently
        selectionIndicator.SetActive(isSelected);
        _selected = isSelected;
    }

    public bool IsSelected()
    {
        return _selected;
    }
}
