using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FullscreenButtons : MonoBehaviour
{
    [SerializeField] private Button onButton;
    [SerializeField] private Button offButton;
    [SerializeField] private Color selected;
    [SerializeField] private Color deselected;

    private bool _isOn;

    public void SetButtons(bool isOn) {
        _isOn = isOn;
        Text onText = onButton.GetComponentInChildren<Text>();
        Text offText = offButton.GetComponentInChildren<Text>();
        if (isOn) {
            onText.color = selected;
            offText.color = deselected;
        } else {
            onText.color = deselected;
            offText.color = selected;
        }
    }

    public bool IsOn() {
        return _isOn;
    }
}
