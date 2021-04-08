using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private GameObject levelListContent;
    [SerializeField] private GameObject levelContainerPrefab;

    private int _indexOfSelectedLevel;
    private Text _name;
    private Image _image;
    private Text _description;

    void Start() {
        _indexOfSelectedLevel = PlayerPrefs.GetInt("LevelIndex", 0);
        _name = transform.Find("Name").gameObject.GetComponent<Text>();
        _image = transform.Find("Image").gameObject.GetComponent<Image>();
        _description = transform.Find("Description").gameObject.GetComponent<Text>();

        InitLevelsScrollView();
        UpdateLevelHolder();
    }

    void Update() {
        UpdateLevelHolder();
        HighlightSelected();
    }

    // Init scroll view with levels
    private void InitLevelsScrollView() {
        RectTransform lvlrt = levelContainerPrefab.transform.GetComponent<RectTransform>();
        float levelContainerHeight = lvlrt.sizeDelta.y * lvlrt.localScale.y;
        float levelListHeight = levelContainerHeight * SceneData.campaignLevels.Count;
        levelListContent.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(0, levelListHeight);
        for (int i = 0; i < SceneData.campaignLevels.Count; i++) {
            CreateLevelContainer(i, levelContainerHeight);
        }
    }

    // Create level container
    private void CreateLevelContainer(int i, float offsetStep) {
        GameObject level = Instantiate(levelContainerPrefab, levelListContent.transform);
        string name = SceneData.campaignLevels[i].gameSettings.mapName;
        level.name = name;
        level.GetComponent<Text>().text = name;
        float offset = -(i * offsetStep);
        level.GetComponent<RectTransform>().localPosition = new Vector3(0, offset, 0);
        level.GetComponent<Button>().onClick.AddListener(() => UpdateSelectedLevel(i));
    }

    public void UpdateSelectedLevel(int i) {
        _indexOfSelectedLevel = i;
    }

    private void HighlightSelected() {
        for (int i = 0; i < levelListContent.transform.childCount; i++) {
            Text text = levelListContent.transform.GetChild(i).gameObject.GetComponent<Text>();
            if (i == _indexOfSelectedLevel) {
                text.fontStyle = FontStyle.Bold;
                text.color = new Color(1, 1, 1, 1);
            } else {
                text.fontStyle = FontStyle.Normal;
                text.color = new Color(0.9f, 0.9f, 0.9f, 1);
            }
        }
    }

    // Save level settings to PlayerPrefs
    private void ApplyCampaignSettings(SceneData.GameSettings gameSettings) {
        PlayerPrefs.SetInt("LevelIndex", _indexOfSelectedLevel);
        SceneData.SaveGameSettings(gameSettings, SceneData.Type.Campaign);
    }

    // Set level holder to display selected level in dropdown
    public void UpdateLevelHolder() {
        SceneData.Level selected = SceneData.campaignLevels[_indexOfSelectedLevel];
        _name.text = selected.gameSettings.mapName;
        _image.sprite = menuManager.GetSceneSprite(selected.gameSettings.spriteName);
        _description.text = selected.description;
    }

    // Start the selected level
    public void Play() {
        ApplyCampaignSettings(SceneData.campaignLevels[_indexOfSelectedLevel].gameSettings);
        menuManager.Play(SceneData.campaignLevels[_indexOfSelectedLevel].gameSettings);
    }
}
