using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class PlayerContainer : MonoBehaviour
{
    [SerializeField] int id;
    [SerializeField] int defaultColorIndex;
    [SerializeField] Toggle[] colorToggles;

    private InputField _nameInput;
    private ToggleGroup _colorSelect;
    private InputField _boinInput;

    private string _defaultName;
    private int _defaultBoins = 10000;

    private string _prefix = SceneData.Type.Multiplayer.ToString();

    void Start() {
        _defaultName = "Player " + id.ToString();

        _nameInput = transform.GetChild(0).gameObject.GetComponent<InputField>();
        _colorSelect = transform.GetChild(1).gameObject.GetComponent<ToggleGroup>();
        _boinInput = transform.GetChild(2).gameObject.GetComponent<InputField>();

        _nameInput.text = PlayerPrefs.GetString(_prefix + "Player " + id.ToString(), _defaultName);
        SetSelectedColor(PlayerPrefs.GetInt(_prefix + "ColorIndex " + id.ToString(), defaultColorIndex));
        _boinInput.text = PlayerPrefs.GetInt(_prefix + "Boins " + id.ToString(), _defaultBoins).ToString();
    }

    private void SetSelectedColor(int i) {
        colorToggles[i].isOn = true;
    }

    public int GetId() {
        return id;
    }

    public string GetName() {
        return _nameInput.text;
    }

    public Color GetColor() {
        foreach (Toggle toggle in _colorSelect.ActiveToggles()) {
            if (toggle.isOn) {
                return toggle.gameObject.transform.GetChild(1).gameObject.GetComponent<Image>().color;
            }
        }
        return Color.white;
    }

    public int GetBoins() {
        return int.Parse(_boinInput.text);
    }

    public void SaveColorIndex() {
        for (int i = 0; i < colorToggles.Length; i++) {
            if (colorToggles[i].isOn) {
                PlayerPrefs.SetInt(_prefix + "ColorIndex " + id.ToString(), i);
                return;
            }
        }
    }
}
