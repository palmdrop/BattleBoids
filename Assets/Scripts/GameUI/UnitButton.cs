using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UnitButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image border;
    [SerializeField] private Color borderHoverColor;

    private Color _borderNormalColor;

    // Will be called when the pointer the button
    private Action _onEnter;

    // Will be called when the pointer exits the button
    private Action _onExit;

    void Start()
    {
        _borderNormalColor = border.color;
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
        HoverEffect(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _onExit.Invoke();
        HoverEffect(false);
    }

    private void HoverEffect(bool set) {
        border.color = set && GetComponent<Button>().interactable ? borderHoverColor : _borderNormalColor;
    }
}
