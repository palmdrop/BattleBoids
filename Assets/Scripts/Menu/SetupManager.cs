using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SetupManager : MenuManager
{
    [SerializeField] private GameObject mapDropdown;
    [SerializeField] private GameObject mapName;
    [SerializeField] private GameObject mapImage;
    [SerializeField] private GameObject playerContainerPrefab;
    [SerializeField] private GameObject playerListContent;
    [SerializeField] private GameObject boins;
    [SerializeField] private GameObject allowedUnits;
    [SerializeField] private GameObject colorPopup;

    private string _defaultPlayerName = "Player ";
    private int _defaultBoins = 10000;
    private Color[] _defaultColors;

    // Struct for holding multiplayer maps
    // NOTE This must hold: name == scene name == sprite name
    // Scene name is the name of the scene in /Assets/Scenes/
    // Sprite name is the name of the sprite to show in the multiplayer menu
    // Put the sprite in /Assests/Resources/Sprite/MapSprites/
    public struct Map {
        public string name;
        public int numberOfPlayers;
    }

    private List<Map> _maps = new List<Map>();

    // Struct for the settings to start the match with
    public struct GameSettings {
        public string mapName;
        public List<PlayerSettings> playerSettingsList;
        public Options options;
    }

    public struct PlayerSettings {
        public int id;
        public string nickname;
        public Color color;
    }

    public struct Options {
        public int boins;
        public Dictionary<string, bool> units;
    }

    void Start() {
        InitColors();
        AddMaps();
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

    // Available maps to play in multiplayer menu
    private void AddMaps() {
        _maps.Add(
            new Map {
                name = "LevelOne",
                numberOfPlayers = 2
            }
        );
    }

    // Get the sprite that corresponds to the map
    private Sprite GetMapSprite(string map) {
        StringBuilder path = new StringBuilder("Sprites/MapSprites/");
        path.Append(map);
        return Resources.Load<Sprite>(path.ToString());
    }

    // Init map dropdown with maps
    private void InitDropdownOptions() {
        Dropdown dd = mapDropdown.GetComponent<Dropdown>();
        dd.ClearOptions();
        Dropdown.OptionData option;
        foreach (Map map in _maps) {
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
        boins.GetComponentInChildren<InputField>().text = PlayerPrefs.GetInt("Boins", _defaultBoins).ToString();
        SetUnits();
    }

    // Update map holder and player holder
    public void UpdateHolders() {
        // Map holder, set name and sprite
        Map selected = _maps[mapDropdown.GetComponent<Dropdown>().value];
        mapName.GetComponent<Text>().text = selected.name;
        mapImage.GetComponent<Image>().sprite = GetMapSprite(selected.name);

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
            PlayerPrefs.GetString(_defaultPlayerName + number, _defaultPlayerName + number);
        GameObject colorButton = player.transform.GetChild(2).gameObject;
        colorButton.GetComponent<Button>().onClick.AddListener(() => ColorSelector(colorButton));
        string defaultColor = "#" + ColorUtility.ToHtmlStringRGBA(i < _defaultColors.Length ? _defaultColors[i] : Color.white);
        Color selectedColor;
        ColorUtility.TryParseHtmlString(PlayerPrefs.GetString("Color " + number, defaultColor), out selectedColor);
        colorButton.GetComponent<Image>().color = selectedColor;
    }

    // Get the game settings from the menu
    private GameSettings GetGameSettings() {
        return new GameSettings {
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
    private List<PlayerSettings> GetPlayerSettingsList() {
        List<PlayerSettings> playerSettings = new List<PlayerSettings>();
        foreach (Transform child in playerListContent.transform) {
            playerSettings.Add(GetPlayerSettings(child.gameObject));
        }
        return playerSettings;
    }

    // Get player settings from a player container
    private PlayerSettings GetPlayerSettings(GameObject playerContainer) {
        int id = int.Parse(playerContainer.transform.GetChild(0).gameObject.GetComponent<Text>().text);
        string nickname = playerContainer.transform.GetChild(1).gameObject.GetComponent<InputField>().text;
        Color color = playerContainer.transform.GetChild(2).gameObject.GetComponent<Image>().color;
        return new PlayerSettings {
            id = id,
            nickname = nickname,
            color = color
        };
    }

    // Get options from menu
    private Options GetOptions() {
        return new Options {
            boins = GetBoins(),
            units = GetUnits()
        };
    }

    // Get boins from menu
    private int GetBoins() {
        return int.Parse(boins.GetComponentInChildren<InputField>().text);
    }

    private void SetUnits() {
        foreach (Transform child in allowedUnits.transform) {
            child.gameObject.GetComponent<Toggle>().isOn =
                Boolean.Parse(PlayerPrefs.GetString(child.gameObject.name, "True"));
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

    // Save game settings to playerprefs
    private void SaveGameSettings() {
        GameSettings gameSettings = GetGameSettings();

        // Selected map
        PlayerPrefs.SetString("Map", gameSettings.mapName);
        PlayerPrefs.SetInt("MapIndex", mapDropdown.GetComponent<Dropdown>().value);

        // Player settings
        foreach (PlayerSettings playerSettings in gameSettings.playerSettingsList) {
            PlayerPrefs.SetString(
                _defaultPlayerName + playerSettings.id.ToString(),
                playerSettings.nickname
            );
            PlayerPrefs.SetString(
                "Color " +  playerSettings.id.ToString(),
                "#" + ColorUtility.ToHtmlStringRGBA(playerSettings.color)
            );
        }

        // Options
        PlayerPrefs.SetInt("Boins", gameSettings.options.boins);
        foreach (KeyValuePair<string, bool> entry in gameSettings.options.units) {
            PlayerPrefs.SetString(entry.Key, entry.Value.ToString());
        }
    }

    // Popup window for selecting color
    public void ColorSelector(GameObject button) {
        colorPopup.GetComponent<ColorPopup>().SetSourceButton(button);
        colorPopup.transform.position = button.transform.position;
        colorPopup.SetActive(true);
    }

    // Start a match with the selected options
    public void Play() {
        SaveGameSettings();
        LoadingScreen();
        IEnumerator loadSceneAsync = LoadSceneAsync(PlayerPrefs.GetString("Map"));
        StartCoroutine(loadSceneAsync);
    }
}
