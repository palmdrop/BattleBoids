using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Will be called when the pointer the button
    private Action _onEnter;

    // Will be called when the pointer exits the button
    private Action _onExit;

    void Start()
    {
    }

    void Update()
    {
    }

    public void SetOnEnter(Action callback)
    {
        this._onEnter = callback;
    }

    public void SetOnExit(Action callback)
    {
        this._onExit = callback;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _onEnter.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _onExit.Invoke();
    }
}