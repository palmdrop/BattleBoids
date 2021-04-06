using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AllowedBoidToggle : MonoBehaviour
{
    [SerializeField] Color selected;
    [SerializeField] Color deselected;

    public void UpdateVisualStatus() {
        Toggle toggle = GetComponent<Toggle>();
        Image image = transform.GetChild(0).gameObject.GetComponent<Image>();
        if (toggle.isOn) {
            image.color = selected;
        } else {
            image.color = deselected;
        }
    }
}
