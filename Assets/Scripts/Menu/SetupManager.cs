using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SetupManager : MonoBehaviour
{
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private GameObject mapDropdown;
    [SerializeField] private GameObject mapName;
    [SerializeField] private GameObject mapImage;
    [SerializeField] private GameObject playerContainerPrefab;
    [SerializeField] private GameObject playerListContent;
    [SerializeField] private GameObject boins;
    [SerializeField] private GameObject allowedUnits;
    [SerializeField] private GameObject colorPopup;

    private string _prefix = SceneData.Type.Multiplayer.ToString();

    private string _defaultPlayerName = "Player ";
    private int _defaultBoins = 10000;
    private Color[] _defaultColors;

    void Start() {
        InitColors();
        InitDropdownOptions();
        UpdateHolders();
        InitOptions();
    }

    // Set color array
    private void InitColors() {
        int numberOfColors = colorPopup.transform.childCount;
        _defaultColors = new Color[numberOfColors];
        for (int i = 0; i < numberOfColors; i++) {
            _defaultColors[i] = colorPopup.transform.GetChild(i).gameObject.GetComponent<Image>().color;
        }
    }

    // Init map dropdown with maps
    private void InitDropdownOptions() {
        Dropdown dd = mapDropdown.GetComponent<Dropdown>();
        dd.ClearOptions();
        Dropdown.OptionData option;
        foreach (SceneData.Map map in SceneData.multiplayerMaps) {
            option = new Dropdown.OptionData();
            StringBuilder entry = new StringBuilder(map.name);
            entry.Append(" (").Append(map.numberOfPlayers.ToString()).Append(")");
            option.text = entry.ToString();
            dd.options.Add(option);
        }
        dd.value = PlayerPrefs.GetInt("MapIndex", 0);
        dd.captionText.text = dd.options[dd.value].text;
    }

    // Init options with default values
    private void InitOptions() {
        boins.GetComponentInChildren<InputField>().text = PlayerPrefs.GetInt(_prefix + "Boins", _defaultBoins).ToString();
        SetUnits();
    }

    // Update map holder and player holder
    public void UpdateHolders() {
        // Map holder, set name and sprite
        SceneData.Map selected = SceneData.multiplayerMaps[mapDropdown.GetComponent<Dropdown>().value];
        mapName.GetComponent<Text>().text = selected.name;
        mapImage.GetComponent<Image>().sprite = menuManager.GetSceneSprite(selected.name);

        // Player holder, set content height and create player containers
        RectTransform pcrt = playerContainerPrefab.transform.GetComponent<RectTransform>();
        float playerContainerHeight = pcrt.sizeDelta.y * pcrt.localScale.y;
        float playerListHeight = playerContainerHeight * selected.numberOfPlayers;
        playerListContent.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(0, playerListHeight);
        for (int i = 0; i < selected.numberOfPlayers; i++) {
            CreatePlayerContainer(i, playerContainerHeight);
        }
    }

    // Create player container
    private void CreatePlayerContainer(int i, float offsetStep) {
        GameObject player = Instantiate(playerContainerPrefab, playerListContent.transform);
        float offset = -(i * offsetStep);
        player.GetComponent<RectTransform>().localPosition = new Vector3(0, offset, 0);
        string number = (i + 1).ToString();
        player.transform.GetChild(0).gameObject.GetComponent<Text>().text = number;
        player.transform.GetChild(1).gameObject.GetComponent<InputField>().text =
            PlayerPrefs.GetString(_prefix + "Player " + number, _defaultPlayerName + number);
        GameObject colorButton = player.transform.GetChild(2).gameObject;
        colorButton.GetComponent<Button>().onClick.AddListener(() => ColorSelector(colorButton));
        string defaultColor = "#" + ColorUtility.ToHtmlStringRGBA(i < _defaultColors.Length ? _defaultColors[i] : Color.white);
        Color selectedColor;
        ColorUtility.TryParseHtmlString(PlayerPrefs.GetString(_prefix + "Color " + number, defaultColor), out selectedColor);
        colorButton.GetComponent<Image>().color = selectedColor;
    }

    // Get the game settings from the menu
    private SceneData.GameSettings GetGameSettings() {
        return new SceneData.GameSettings {
            mapName = GetMapName(),
            playerSettingsList = GetPlayerSettingsList(),
            options = GetOptions()
        };
    }

    // Get name of the selected map
    private string GetMapName() {
        return mapName.GetComponent<Text>().text;
    }

    // Get list of player settings from the menu
    private List<SceneData.PlayerSettings> GetPlayerSettingsList() {
        List<SceneData.PlayerSettings> playerSettings = new List<SceneData.PlayerSettings>();
        foreach (Transform child in playerListContent.transform) {
            playerSettings.Add(GetPlayerSettings(child.gameObject));
        }
        return playerSettings;
    }

    // Get player settings from a player container
    private SceneData.PlayerSettings GetPlayerSettings(GameObject playerContainer) {
        int id = int.Parse(playerContainer.transform.GetChild(0).gameObject.GetComponent<Text>().text);
        string nickname = playerContainer.transform.GetChild(1).gameObject.GetComponent<InputField>().text;
        Color color = playerContainer.transform.GetChild(2).gameObject.GetComponent<Image>().color;
        return new SceneData.PlayerSettings {
            id = id,
            nickname = nickname,
            color = color
        };
    }

    // Get options from menu
    private SceneData.Options GetOptions() {
        return new SceneData.Options {
            boins = GetBoins(),
            units = GetUnits()
        };
    }

    // Get boins from menu
    private int GetBoins() {
        return int.Parse(boins.GetComponentInChildren<InputField>().text);
    }

    // Set allowed units in menu
    private void SetUnits() {
        foreach (Transform child in allowedUnits.transform) {
            child.gameObject.GetComponent<Toggle>().isOn =
                Boolean.Parse(PlayerPrefs.GetString(_prefix + child.gameObject.name, "True"));
        }
    }

    // Get allowed units from the menu
    private Dictionary<string, bool> GetUnits() {
        Dictionary<string, bool> units = new Dictionary<string, bool>();
        foreach (Transform child in allowedUnits.transform) {
            units.Add(child.gameObject.name, child.gameObject.GetComponent<Toggle>().isOn);
        }
        return units;
    }

    // Save selected game settings to playerprefs
    private void ApplyMultiplayerSettings(SceneData.GameSettings gameSettings) {
        PlayerPrefs.SetInt("MapIndex", mapDropdown.GetComponent<Dropdown>().value);
        SceneData.SaveGameSettings(gameSettings, SceneData.Type.Multiplayer);
    }

    // Popup window for selecting color
    public void ColorSelector(GameObject button) {
        colorPopup.GetComponent<ColorPopup>().SetSourceButton(button);
        colorPopup.transform.position = button.transform.position;
        colorPopup.SetActive(true);
    }

    // Start a match with the selected options
    public void Play() {
        ApplyMultiplayerSettings(GetGameSettings());
        menuManager.Play(PlayerPrefs.GetString("Scene"));
    }
}
