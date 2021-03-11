using System;
using System.ComponentModel;
using UnityEngine;

public abstract class Selectable : MonoBehaviour, ISelectable
{
    protected int Cost;
    
    [SerializeField] private GameObject selectionIndicatorPrefab;
    private GameObject selectionIndicator;
    private bool _selected = false;

    private Vector3 offset;
    private Vector3 positionBeforeMoved;

    private Transform selectionIndicatorTransform;
    private Transform thisTransform;
    private void Awake()
    {
        thisTransform = transform; 
        selectionIndicator = Instantiate(selectionIndicatorPrefab, Vector3.zero, Quaternion.identity);
        selectionIndicatorTransform = selectionIndicator.transform;
        
        selectionIndicatorTransform.parent = thisTransform;
        selectionIndicatorTransform.position = thisTransform.localPosition;
    }

    // Adds the selected visualisation 
    public void SetSelectionIndicator(bool isSelected)
    {
        selectionIndicator.SetActive(isSelected);
        _selected = isSelected;
    }

    public void SetOffset(Vector3 anchor)
    {
        offset = transform.position - anchor;
    }

    public Vector3 GetOffset()
    {
        return offset;
    }

    public void SetPositionBeforeMoved()
    {
        positionBeforeMoved = transform.position;
    }

    public Vector3 GetPositionBeforeMoved()
    {
        return positionBeforeMoved;
    }
    
    public int GetCost()
    {
        return Cost;
    }
    
    public bool IsSelected()
    {
        return _selected;
    }
}
