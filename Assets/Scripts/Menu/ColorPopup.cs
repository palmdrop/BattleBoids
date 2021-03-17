using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorPopup : MonoBehaviour
{
    private GameObject _sourceButton;

    public void SetSourceButton(GameObject button) {
        _sourceButton = button;
    }

    // Set color of source button to clicked color
    public void OnButtonClick (GameObject button) {
        Color color = button.GetComponent<Image>().color;
        if (_sourceButton != null) {
            _sourceButton.GetComponent<Image>().color = color;
        }
        gameObject.SetActive(false);
    }
}
