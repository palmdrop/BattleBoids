using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Campaign or Multiplayer scene?
    [SerializeField] private SceneData.Type type;

    // The possible boid types
    public enum GameState {
        Placement,
        Running,
        Victory,    
        Paused,
    }

    private GameUI _gameUI;
    private BoidManager _boidManager;

    private GameState _state;
    private bool _paused;
    private List<Player> _players;

    [SerializeField] private Camera mainCamera;


    // Start is called before the first frame update
    void Start()
    {
        _state = GameState.Placement;
        _gameUI = GetComponentInChildren<GameUI>();
        _boidManager = GetComponentInChildren<BoidManager>();
        _players = new List<Player>(GetComponentsInChildren<Player>());
        AudioManager.instance.PlayMusic("MenuMusic");
        ApplyPlayerSettings();
    }

    // Update is called once per frame
    void Update()
    {
        var alivePlayers = _players.FindAll(p => p.GetFlock().FindAll(b => !b.IsDead()).Count > 0);
        if (_state == GameState.Running && alivePlayers.Count == 1) {
            SetState(GameState.Victory);
            _gameUI.ShowVictor(alivePlayers[0]);
        }
    }

    // Apply PlayerPrefs data to player if it exists
    private void ApplyPlayerSettings() {
        string prefix = type.ToString();
        for (int i = 0; i < _players.Count; i++) {
            int number = i + 1;
            string player = prefix + "Player " + number.ToString();
            if (PlayerPrefs.HasKey(player)) {
                _players[i].SetId(number);
                _players[i].SetNickname(PlayerPrefs.GetString(player));
                Color color;
                ColorUtility.TryParseHtmlString(PlayerPrefs.GetString(prefix + "Color " + number.ToString()), out color);
                _players[i].SetColor(color);
                _players[i].SetBoins(PlayerPrefs.GetInt(prefix + "Boins " + number.ToString()));
            }
        }
    }

    public void BeginBattle() {
        // Have to check this because it is called every frame when the battle is running
        if (_state != GameState.Running)
        {
            AudioManager.instance.StopMusic("MenuMusic");
            AudioManager.instance.PlayMusic("BattleMusic");
        }
        _state = GameState.Running;
        _boidManager.AddPlayerBoids();
        foreach (Player p in _players) {
            p.SetActive(false);
        }

        GetComponentInChildren<SelectionManager>().gameObject.SetActive(false);

    }

    public SceneData.Type GetType() {
        return type;
    }

    public void SetState(GameState state)
    {
        _state = state;
    }

    public GameState GetState()
    {
        return _state;
    }

    public List<Player> GetPlayers()
    {
        return _players;
    }

    public GameUI GetGameUI()
    {
        return _gameUI;
    }

    public Camera GetMainCamera()
    {
        return mainCamera;
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
    }

    public bool IsPaused()
    {
        return _paused;
    }

}
