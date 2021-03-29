using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    [SerializeField] private Text boins;
    [SerializeField] private Dropdown playerSelect;
    [SerializeField] private Player activePlayer;
    [SerializeField] private Canvas buttons;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private List<GameObject> unitPrefabs; // Use same order for
    [SerializeField] private List<Sprite> unitSprites;     // unitPrefabs and unitSprites
    [SerializeField] private Button ready;
    [SerializeField] private int unitButtonRows;
    [SerializeField] private int unitButtonCols;
    [SerializeField] private bool showHealthBars;
    [SerializeField] private Text victoryText;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject victoryMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject optionsMenu;

    private GameManager _gameManager;
    private string _prefix;
    private bool hasStarted = false;
    private List<Player> players;

    // Start is called before the first frame update
    void Start()
    {
        _gameManager = GetComponentInParent<GameManager>();
        _prefix = _gameManager.GetType().ToString();
        players = _gameManager.GetPlayers();
        InitPlayerDropdown();
        InitUnitButtons();
        InitReadyButton();
        InitVictoryMenu();
        Resume();
    }

    // Update is called once per frame
    void Update()
    {
        ManageKeyInput();
        UpdateBoins();
        UpdateReady();
        UpdateButtonColors(activePlayer.color);
        UpdateGameState();
    }

    void InitPlayerDropdown()
    {
        if (_gameManager.GetType() != SceneData.Type.Multiplayer)
        {
            playerSelect.gameObject.SetActive(false);
            activePlayer.SetActive(true);
            return;
        }
        playerSelect.ClearOptions();
        foreach (var player in players)
        {
            Dropdown.OptionData newPlayer = new Dropdown.OptionData();
            newPlayer.text = player.GetNickname();
            playerSelect.options.Add(newPlayer);
        }
        activePlayer = SetActivePlayer();
        playerSelect.captionText.text = activePlayer.GetNickname();
        playerSelect.onValueChanged.AddListener(delegate {
            ManageActivePlayer();
        });
    }

    void InitUnitButtons()
    {
        for (int i = 0; i < unitPrefabs.Count; i++)
        {
            GameObject button = Instantiate(buttonPrefab);
            button.transform.SetParent(buttons.transform);
            button.name = unitPrefabs[i].name;
            RectTransform buttonRectTransform = button.transform.GetComponent<RectTransform>();
            buttonRectTransform.localScale = new Vector3(1, 1, 1);
            float width = buttonRectTransform.sizeDelta.x * buttonRectTransform.localScale.x;
            float height = buttonRectTransform.sizeDelta.y * buttonRectTransform.localScale.y;
            button.transform.localPosition = new Vector3(
                -(i % unitButtonCols) * width,
                (i % unitButtonRows) * height,
                0
            );

            Image image = button.GetComponent<Image>();
            image.sprite = unitSprites[i];
            Color color = activePlayer.color;
            if (Boolean.Parse(PlayerPrefs.GetString(_prefix + unitPrefabs[i].name, "true"))) {
                button.GetComponent<Button>().onClick.AddListener(() => UnitButtonClick(button));
            } else {
                color = GetDisableColor(color);
                Destroy(button.GetComponent<Button>());
            }
            image.color = color;
        }
    }

    void InitReadyButton()
    {
        UpdateReady();
        ready.onClick.AddListener(ToggleReady);
    }

    void InitVictoryMenu()
    {
        victoryMenu.gameObject.SetActive(false);
        backButton.onClick.AddListener(GoBack);
    }

    void ManageKeyInput() {
        if (Input.GetKey("1")) {
            // Select player 1
            SetPlayerSelectValue(0);
        } else if (Input.GetKey("2")) {
            // Select player 2
            SetPlayerSelectValue(1);
        } else if (Input.GetKeyDown("r"))
        {
            // Run game
            players.ForEach(p => p.Ready());
        } else if (Input.GetKeyDown("y")) {
            showHealthBars = !showHealthBars;
        } else if (Input.GetKeyDown("m")) {
            AudioManager.instance.ToggleMute();
        }
        else if (Input.GetKeyDown("u"))
        {
            AudioManager.instance.SetMasterVolume(AudioManager.instance.GetMasterVolume() + 0.1f);
        }
        else if (Input.GetKeyDown("j"))
        {
            AudioManager.instance.SetMasterVolume(AudioManager.instance.GetMasterVolume() - 0.1f);
        }
        else if (Input.GetKeyDown("i"))
        {
            AudioManager.instance.SetSoundEffectsVolume(AudioManager.instance.GetSoundEffectsVolume() + 0.1f);
        }
        else if (Input.GetKeyDown("k"))
        {
            AudioManager.instance.SetSoundEffectsVolume(AudioManager.instance.GetSoundEffectsVolume() - 0.1f);
        }
        else if (Input.GetKeyDown("o"))
        {
            AudioManager.instance.SetMusicVolume(AudioManager.instance.GetMusicVolume() + 0.1f);
        }
        else if (Input.GetKeyDown("l"))
        {
            AudioManager.instance.SetMusicVolume(AudioManager.instance.GetMusicVolume() - 0.1f);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!_gameManager.IsPaused()) {
                Pause();
            } else {
                Resume();
            }
        }
    }

    void ManageActivePlayer() {
        string selectedPlayerName = playerSelect.options[playerSelect.value].text;
        string activePlayerName = activePlayer.GetNickname();
        if (!selectedPlayerName.Equals(activePlayerName)) {
            activePlayer = SetActivePlayer();
        }
    }

    void UpdateReady()
    {
        if (activePlayer.IsReady())
        {
            ready.GetComponentInChildren<Text>().text = "Unready";
        }
        else
        {
            ready.GetComponentInChildren<Text>().text = "Ready";
        }

        if (!activePlayer.CanReady())
        {
            ready.interactable = false;
        }
        else
        {
            ready.interactable = true;
        }
    }

    void UpdateGameState() {
        if (AllPlayersReady() && !hasStarted) {
            _gameManager.BeginBattle();
            ready.gameObject.SetActive(false);
            hasStarted = true;
        }
    }

    void UpdateButtonColors(Color color) {
        foreach (Transform child in buttons.transform) {
            UpdateButtonColor(child.gameObject, color);
        }
    }

    void UpdateButtonColor(GameObject unitButton, Color color) {
        if (!Boolean.Parse(PlayerPrefs.GetString(_prefix + unitButton.name, "true"))) {
            color = GetDisableColor(color);
        }
        unitButton.GetComponent<Image>().color = color;
    }

    Color GetDisableColor(Color color) {
        return new Color(color.r, color.g, color.b, 0.25f);
    }

    void ToggleReady()
    {
        if (activePlayer.IsReady())
        {
            activePlayer.Unready();
        }
        else
        {
            activePlayer.Ready();
        }
    }

    public void GoBack()
    {
        AudioManager.instance.StopMusic("BattleMusic");
        SceneManager.LoadScene("Menu");
    }

    void UnitButtonClick(GameObject button)
    {
        activePlayer.GetSpawnArea().SetEntityToSpawn(FindUnitByName(button.name));
        activePlayer.GetSpawnArea().SetPlacing(true);
    }

    GameObject FindUnitByName(string name)
    {
        foreach (GameObject unit in unitPrefabs)
        {
            if (unit.name.Equals(name))
            {
                return unit;
            }
        }
        return null;
    }

    void SetPlayerSelectValue(int i) {
        if (i >= 0 && i - 1 <= playerSelect.options.Count) {
            playerSelect.value = i;
        }
    }

    Player SetActivePlayer()
    {
        string nickname = playerSelect.options[playerSelect.value].text;
        foreach (Player player in players)
        {
            player.SetActive(false);
        }
        foreach (Player player in players)
        {
            if (player.GetNickname().Equals(nickname))
            {
                player.SetActive(true);
                return player;
            }
        }
        return null;
    }

    public void UpdateBoins()
    {
        boins.text = activePlayer.GetBoins().ToString();
    }

    public Player GetActivePlayer()
    {
        return activePlayer;
    }

    public bool AllPlayersReady()
    {
        foreach (Player player in players)
        {
            if (!player.IsReady())
            {
                return false;
            }
        }
        return true;
    }

    public Button GetReadyButton()
    {
        return ready;
    }

    public bool ShowHealthBars()
    {
        return showHealthBars;
    }

    public void ShowVictor(Player victor)
    {
        AudioManager.instance.StopMusic("BattleMusic");
        AudioManager.instance.PlayMusic("Fanfare");
        victoryText.text = victor.GetNickname() + " won!";
        victoryMenu.SetActive(true);
    }

    public void Pause()
    {
        gameUI.SetActive(false);
        pauseMenu.SetActive(true);
        optionsMenu.SetActive(false);
        _gameManager.SetPaused(true);
    }

    public void Resume()
    {
        gameUI.SetActive(true);
        optionsMenu.SetActive(false);
        pauseMenu.SetActive(false);
        _gameManager.SetPaused(false);
    }

    public void OpenOptions()
    {
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
