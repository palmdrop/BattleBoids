using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    private GameManager _gameManager;
    private bool hasStarted = false;
    private List<Player> players;

    // Start is called before the first frame update
    void Start()
    {
        _gameManager = GetComponentInParent<GameManager>();
        players = _gameManager.GetPlayers();
        InitPlayerDropdown();
        InitUnitButtons();
        InitReadyButton();
        victoryText.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        ManageKeyInput();
        UpdateBoins();
        UpdateReady();
        UpdateGameState();
    }

    void InitPlayerDropdown()
    {
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

            button.GetComponent<Image>().sprite = unitSprites[i];
            string prefix = _gameManager.GetType().ToString();
            if (Boolean.Parse(PlayerPrefs.GetString(prefix + unitPrefabs[i].name, "true"))) {
                button.GetComponent<Button>().onClick.AddListener(() => UnitButtonClick(button));
            } else {
                Color disabled = new Color(1f, 1f, 1f, 0.25f);
                button.GetComponent<Image>().color = disabled;
                Destroy(button.GetComponent<Button>());
            }
        }
    }

    void InitReadyButton()
    {
        foreach (Player player in players)
        {
            player.Unready();
        }
        UpdateReady();
        ready.onClick.AddListener(ToggleReady);
    }

    void ManageKeyInput() {
        if (Input.GetKey("1")) {
            // Select player 1
            SetPlayerSelectValue(0);
        } else if (Input.GetKey("2")) {
            // Select player 2
            SetPlayerSelectValue(1);
        } else if (Input.GetKeyDown("n")) {
            // Create new entity
            activePlayer.GetSpawnArea().ChangeGridWidth(1);
        } else if (Input.GetKeyDown("x")) {
            // Remove entity
            activePlayer.GetSpawnArea().ChangeGridWidth(-1);
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
    }

    void UpdateGameState() {
        if (AllPlayersReady() && !hasStarted) {
            _gameManager.BeginBattle();
            ready.gameObject.SetActive(false);
            hasStarted = true;
        }
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

    void UnitButtonClick(GameObject button)
    {
        activePlayer.GetSpawnArea().SetEntityToSpawn(FindUnitByName(button.name));
        activePlayer.GetSpawnArea().ChangeGridWidth(1);
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
        victoryText.text = victor.GetNickname() + " won!";
        victoryText.enabled = true;
    }
}
