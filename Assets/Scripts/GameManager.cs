using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
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
    private List<Player> _players;


    // Start is called before the first frame update
    void Start()
    {
        _state = GameState.Placement;
        _gameUI = GetComponentInChildren<GameUI>();
        _boidManager = GetComponentInChildren<BoidManager>();
        _players = new List<Player>(GetComponentsInChildren<Player>());
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

    public void BeginBattle() {
        _state = GameState.Running;
        _boidManager.AddPlayerBoids();
        foreach (Player p in _players) {
            p.SetActive(false);
        }
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
        return Camera.main;
    }

}
