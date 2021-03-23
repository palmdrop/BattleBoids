using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private GameObject dropdown;

    private Dropdown _dd;
    private Text _name;
    private Image _image;
    private Text _description;

    void Start() {
        _dd = dropdown.GetComponent<Dropdown>();
        _name = transform.Find("Name").gameObject.GetComponent<Text>();
        _image = transform.Find("Image").gameObject.GetComponent<Image>();
        _description = transform.Find("Description").gameObject.GetComponent<Text>();

        InitDropdownOptions();
        UpdateLevelHolder();
    }

    // Init Dropdown with levels
    private void InitDropdownOptions() {
        _dd.ClearOptions();
        Dropdown.OptionData option;
        foreach (SceneData.Level level in SceneData.campaignLevels) {
            option = new Dropdown.OptionData();
            option.text = level.gameSettings.mapName;
            _dd.options.Add(option);
        }
        _dd.value = PlayerPrefs.GetInt("LevelIndex", 0);
        _dd.captionText.text = SceneData.campaignLevels[_dd.value].gameSettings.mapName;
    }

    // Save level settings to PlayerPrefs
    private void ApplyCampaignSettings(SceneData.GameSettings gameSettings) {
        PlayerPrefs.SetInt("LevelIndex", _dd.value);
        SceneData.SaveGameSettings(gameSettings, SceneData.Type.Campaign);
    }

    // Set level holder to display selected level in dropdown
    public void UpdateLevelHolder() {
        SceneData.Level selected = SceneData.campaignLevels[_dd.value];
        _name.text = selected.gameSettings.mapName;
        _image.sprite = menuManager.GetSceneSprite(selected.gameSettings.spriteName);
        _description.text = selected.description;
    }

    // Start the selected level
    public void Play() {
        ApplyCampaignSettings(SceneData.campaignLevels[_dd.value].gameSettings);
        menuManager.Play(SceneData.campaignLevels[_dd.value].gameSettings);
    }
}
