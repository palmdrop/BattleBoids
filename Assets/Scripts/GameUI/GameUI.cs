using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    public static GameUI SharedInstance;

    [SerializeField] private Text boins;
    
    [SerializeField] private GameObject currentCost;
    [SerializeField] private Text currentCostText;
    [SerializeField] private GameObject tooltip;
    [SerializeField] private Text tooltipText;
    [SerializeField] private Player activePlayer;
    [SerializeField] private GameObject unitsText;
    [SerializeField] private GameObject unitButtons;
    [SerializeField] private GameObject commadsText;
    [SerializeField] private GameObject commandButtons;
    [SerializeField] private List<GameObject> unitPrefabs;
    [SerializeField] private Button ready;
    [SerializeField] private bool showHealthBars;
    [SerializeField] private Text victoryText;
    [SerializeField] private GameObject nextLevelButton;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject victoryMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private CommandManager commandManager;

    private GameManager _gameManager;
    private string _prefix;
    private bool hasStarted = false;
    private List<Player> players;
    private int _activePlayerId;

    private void Awake()
    {
        SharedInstance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        _gameManager = GetComponentInParent<GameManager>();
        _prefix = _gameManager.GetType().ToString();
        players = _gameManager.GetPlayers();
        tooltip.SetActive(false);
        SetActivePlayerId(1);
        InitUnitButtons();
        InitCommandButtons();
        InitReadyButton();
        InitVictoryMenu();
        Resume();
        RegisterKeyInputs();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateBoins();
        UpdateReady();
        UpdateButtonColors(activePlayer.color);
        UpdateGameState();
    }

    void InitUnitButtons() 
    {
        foreach (Transform child in unitButtons.transform) {
            GameObject button = child.GetChild(1).gameObject;
            Image unitImage = button.GetComponent<Image>();
            Color color = activePlayer.color;
            if (Boolean.Parse(PlayerPrefs.GetString(_prefix + button.name, "true"))) {
                button.GetComponent<Button>().onClick.AddListener(() => UnitButtonClick(button));
            } else {
                button.GetComponent<Button>().interactable = false;
            }

            string tooltipContent = button.name.ToUpper() + "\n" + Boid.GetDescription(button.name);
            button.GetComponent<UnitButton>().SetOnEnter(() => SetTooltip(button, tooltipContent));
            button.GetComponent<UnitButton>().SetOnExit(() => UnsetTooltip()); 
            unitImage.color = color;
        }
    }
    void InitCommandButtons()
    {

        List<(KeyCode, string)> keyCodesAndDescription = commandManager.GETPressedKeyCodesAndDescription();


        // Defaults the actions on the buttons to do nothing
        foreach (Transform child in commandButtons.transform)
        {
            child.GetComponentInChildren<UnitButton>().SetOnEnter(() => {});
            child.GetComponentInChildren<UnitButton>().SetOnExit(() => {});
        }

        // Here we override the buttons with actual actions
        for (int i = 0; i < keyCodesAndDescription.Count; i++)
        {
            // The x variable is just a copy of i, it needs to be used to avoid closure problems when sending
            // it down into the arrow function
            int x = i;
            
            GameObject button = commandButtons.transform.GetChild(i).gameObject;
            button.GetComponentInChildren<Button>().interactable = true;

            button.GetComponentInChildren<Button>().onClick.AddListener(()=>
            {
                Debug.Log(keyCodesAndDescription[x].Item2);
                commandManager.RunActionOnKeyCode(keyCodesAndDescription[x].Item1, KeyCode.None);
            });
            
            
            button.GetComponentInChildren<Text>().text = keyCodesAndDescription[x].Item1.ToString();

            string tooltipContent = keyCodesAndDescription[x].Item2;

            button.GetComponentInChildren<UnitButton>().SetOnEnter(() => SetTooltip(button, tooltipContent));
            button.GetComponentInChildren<UnitButton>().SetOnExit(() => UnsetTooltip()); 
        }
    }
    
    void SetTooltip(GameObject button, string tooltipContent)
    {
        if (!button.GetComponentInChildren<Button>().interactable) return;
        tooltip.SetActive(true);
        tooltipText.text = tooltipContent;
    }

    public static bool IsMouseOverUI()
    {
        return  EventSystem.current.IsPointerOverGameObject();
    }

    void UnsetTooltip()
    {
        tooltip.SetActive(false);
    }

    void InitReadyButton()
    {
        UpdateReady();
        ready.onClick.AddListener(ToggleReady);
    }

    void InitVictoryMenu() {
        bool nextLevelExists = SceneData.GetNextLevel() != null;
        if (_gameManager.GetType() == SceneData.Type.Multiplayer || !nextLevelExists) {
            nextLevelButton.SetActive(false);
        }
    }

    public CommandManager GETCommandManager()
    {
        return commandManager;
    }

    private void RegisterKeyInputs() {
        commandManager.RegisterPressedAction(KeyCode.Alpha1, () => SetActivePlayerId(1), "Change to the first player");
        commandManager.RegisterPressedAction(KeyCode.Alpha2, () => SetActivePlayerId(2), "Change to the second player");
        commandManager.RegisterPressedAction(KeyCode.R, () => players.ForEach(p => p.Ready()), "Start the game");
        commandManager.RegisterPressedAction(KeyCode.Y, () => showHealthBars = !showHealthBars, "Toggle health bars");
        commandManager.RegisterPressedAction(KeyCode.M, () => AudioManager.instance.ToggleMute());
        commandManager.RegisterPressedAction(KeyCode.U, () => AudioManager.instance.SetMasterVolume(AudioManager.instance.GetMasterVolume() + 0.1f));
        commandManager.RegisterPressedAction(KeyCode.J, () => AudioManager.instance.SetMasterVolume(AudioManager.instance.GetMasterVolume() - 0.1f));
        commandManager.RegisterPressedAction(KeyCode.I, () => AudioManager.instance.SetSoundEffectsVolume(AudioManager.instance.GetSoundEffectsVolume() + 0.1f));
        commandManager.RegisterPressedAction(KeyCode.P, () => AudioManager.instance.SetSoundEffectsVolume(AudioManager.instance.GetSoundEffectsVolume() - 0.1f));
        commandManager.RegisterPressedAction(KeyCode.O, () => AudioManager.instance.SetMusicVolume(AudioManager.instance.GetMusicVolume() + 0.1f));
        commandManager.RegisterPressedAction(KeyCode.L, () => AudioManager.instance.SetMusicVolume(AudioManager.instance.GetMusicVolume() - 0.1f));
        commandManager.RegisterPressedAction(KeyCode.Escape, () => PressedEscape(), "Open pause menu");
    }

    public void PressedEscape()
    {
        if (GetActivePlayer().GetSpawnArea().IsInPlacingPhase())
        {
            GetActivePlayer().GetSpawnArea().CancelPlacingPhase();
        }
        else
        {
            if (!_gameManager.IsPaused())
            {
                Pause();
            }
            else
            {
                Resume();
            }
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
            boins.transform.parent.gameObject.SetActive(false);
            unitsText.SetActive(false);
            unitButtons.SetActive(false);
            commadsText.SetActive(false);
            commandButtons.SetActive(false);
            tooltip.SetActive(false);
            hasStarted = true;
        }
    }

    void UpdateButtonColors(Color color) {
        foreach (Transform child in unitButtons.transform) {
            UpdateButtonColor(child.gameObject, color);
        }
    }

    void UpdateButtonColor(GameObject unitButton, Color color) {
        unitButton.transform.GetChild(1).gameObject.GetComponent<Image>().color = color;
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

    public void Menu() {
        AudioManager.instance.StopMusic("BattleMusic");
        SceneManager.LoadScene("Menu");
    }

    public void Replay() {
        AudioManager.instance.StopMusic("BattleMusic");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextLevel() {
        var nextLevel = SceneData.GetNextLevel();
        if (nextLevel != null) {
            SceneData.SaveGameSettings(((SceneData.Level)nextLevel).gameSettings, SceneData.Type.Campaign);
            SceneManager.LoadScene(((SceneData.Level)nextLevel).gameSettings.mapName);
        }
    }

    void UnitButtonClick(GameObject button)
    {
        activePlayer.GetSpawnArea().SetEntityToSpawn(FindUnitByName(button.name));
        activePlayer.GetSpawnArea().SetPlacing(true);
    }

    public GameObject FindUnitByName(string name)
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

    public void SetActivePlayerId(int i) {
        if (_gameManager.GetType() == SceneData.Type.Campaign) {
            _activePlayerId = 1;
        } else {
            _activePlayerId = i;
        }
        if (_gameManager.GetState() == GameManager.GameState.Placement) {
            ManageActivePlayer();
        }
    }

    private void ManageActivePlayer() {
        foreach (Player player in players) {
            if (player.GetId() == _activePlayerId) {
                activePlayer = player;
                player.SetActive(true);
            } else {
                player.SetActive(false);
            }
        }
    }

    private string IntToHumanReadbleNumber(int number) {
        bool isNegative = number < 0;
        string sNumber = number.ToString();
        StringBuilder sbNumber = new StringBuilder();
        int lastIndex = isNegative ? sNumber.Length - 2 : sNumber.Length - 1;
        for (int i = 0; i <= lastIndex; i++) {
            if (i != 0 && i % 3 == 0) {
                sbNumber.Insert(0, ',');
            }
            sbNumber.Insert(0, sNumber[sNumber.Length - 1 - i]);
        }
        if (isNegative) {
            sbNumber.Insert(0, '-');
        }
        return sbNumber.ToString();
    }

    public void UpdateBoins()
    {
        boins.text = IntToHumanReadbleNumber(activePlayer.GetBoins());
        int cost = -activePlayer.GetSpawnArea().SumHoldingCost();
        currentCost.SetActive(cost != 0);
        currentCostText.text = IntToHumanReadbleNumber(cost);
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
