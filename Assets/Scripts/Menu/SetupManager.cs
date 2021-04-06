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
    [SerializeField] private GameObject mapListContent;
    [SerializeField] private GameObject mapContainerPrefab;
    [SerializeField] private Text mapName;
    [SerializeField] private Image mapImage;
    [SerializeField] private PlayerContainer[] players;
    [SerializeField] private GameObject allowedUnits;

    private string _prefix = SceneData.Type.Multiplayer.ToString();

    private string _defaultPlayerName = "Player ";
    private int _defaultBoins = 10000;
    private Color[] _defaultColors;

    private int _indexOfSelectedMap;

    void Start() {
        _indexOfSelectedMap = PlayerPrefs.GetInt("MapIndex", 0);
        InitMapScrollView();
        UpdateMapHolder();
        InitOptions();
    }

    void Update() {
        UpdateMapHolder();
        HighlightSelected();
    }

    // Init map scroll view with maps
    private void InitMapScrollView() {
        RectTransform maprt = mapContainerPrefab.transform.GetComponent<RectTransform>();
        float mapContainerHeight = maprt.sizeDelta.y * maprt.localScale.y;
        float mapListHeight = mapContainerHeight * SceneData.multiplayerMaps.Count;
        mapListContent.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(0, mapListHeight);
        for (int i = 0; i < SceneData.multiplayerMaps.Count; i++) {
            CreateMapContainer(i, mapContainerHeight);
        }
    }

    // Create map container
    private void CreateMapContainer(int i, float offsetStep) {
        GameObject map = Instantiate(mapContainerPrefab, mapListContent.transform);
        string name = SceneData.multiplayerMaps[i].name;
        map.name = name;
        map.GetComponent<Text>().text = name;
        float offset = -(i * offsetStep);
        map.GetComponent<RectTransform>().localPosition = new Vector3(0, offset, 0);
        map.GetComponent<Button>().onClick.AddListener(() => UpdateSelectedMap(i));
    }

    public void UpdateSelectedMap(int i) {
        _indexOfSelectedMap = i;
    }

    private void HighlightSelected() {
        for (int i = 0; i < mapListContent.transform.childCount; i++) {
            Text text = mapListContent.transform.GetChild(i).gameObject.GetComponent<Text>();
            if (i == _indexOfSelectedMap) {
                text.fontStyle = FontStyle.Bold;
                text.color = new Color(1, 1, 1, 1);
            } else {
                text.fontStyle = FontStyle.Normal;
                text.color = new Color(0.9f, 0.9f, 0.9f, 1);
            }
        }
    }

    // Init options
    private void InitOptions() {
        SetUnits();
    }

    // Update map holder
    public void UpdateMapHolder() {
        // set name and sprite
        SceneData.Map selected = SceneData.multiplayerMaps[_indexOfSelectedMap];
        mapName.text = selected.name;
        mapImage.sprite = menuManager.GetSceneSprite(selected.name);
    }

    // Get the game settings from the menu
    private SceneData.GameSettings GetGameSettings() {
        return new SceneData.GameSettings {
            mapName = GetMapName(),
            spriteName = GetMapName(),
            playerSettingsList = GetPlayerSettingsList(),
            options = GetOptions()
        };
    }

    // Get name of the selected map
    private string GetMapName() {
        return mapName.text;
    }

    // Get list of player settings from the menu
    private List<SceneData.PlayerSettings> GetPlayerSettingsList() {
        List<SceneData.PlayerSettings> playerSettings = new List<SceneData.PlayerSettings>();
        foreach (PlayerContainer player in players) {
            playerSettings.Add(GetPlayerSettings(player));
        }
        return playerSettings;
    }

    // Get player settings from a player container
    private SceneData.PlayerSettings GetPlayerSettings(PlayerContainer player) {
        return new SceneData.PlayerSettings {
            id = player.GetId(),
            nickname = player.GetName(),
            color = player.GetColor(),
            boins = player.GetBoins()
        };
    }

    // Get options from menu
    private SceneData.Options GetOptions() {
        return new SceneData.Options {
            units = GetUnits()
        };
    }

    // Set allowed units in menu
    private void SetUnits() {
        foreach (Transform child in allowedUnits.transform) {
            child.gameObject.GetComponent<Toggle>().isOn =
                Boolean.Parse(PlayerPrefs.GetString(_prefix + child.gameObject.name, "true"));
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
        foreach (PlayerContainer player in players) {
            player.SaveColorIndex();
        }
        PlayerPrefs.SetInt("MapIndex", _indexOfSelectedMap);
        SceneData.SaveGameSettings(gameSettings, SceneData.Type.Multiplayer);
    }

    public void SaveCurrentSetup() {
        SceneData.GameSettings gameSettings = GetGameSettings();
        ApplyMultiplayerSettings(gameSettings);
    }

    // Start a match with the selected options
    public void Play() {
        var gameSettings = GetGameSettings();
        ApplyMultiplayerSettings(gameSettings);
        menuManager.Play(gameSettings);
    }
}
