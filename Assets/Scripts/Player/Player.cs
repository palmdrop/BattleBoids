using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Player : MonoBehaviour
{
    public struct FlockInfo
    {
        public float3 avgPos;
        public float3 medianPos;
        public float3 avgVel;
        public int boidCount;
    }
    
    [SerializeField] private List<Boid> flock = new List<Boid>();
    [SerializeField] private int boins; // Currency to buy boids
    [SerializeField] private int score; // Points obtained during a match
    [SerializeField] public int id; // Identifier
    [SerializeField] public Color color; // Player color
    [SerializeField] private string nickname; // Unique
    [SerializeField] private bool ready;

    private GameUI _gameUI;

    private FlockInfo _flockInfo;
    public bool FlockUpdate { get; set; } = false;

    private bool _active = false;

    // Start is called before the first frame update
    void Start()
    {
        _gameUI = GetComponentInParent<GameManager>().GetGameUI();
        foreach (Boid b in GetComponentsInChildren<Boid>())
        {
            flock.Add(b);
        }
        if (flock.Count > 0)
        {
            Ready();
        }
        _flockInfo = new FlockInfo()
        {
            avgPos = float3.zero,
            avgVel = float3.zero,
            boidCount = flock.Count,
        };
    }

    // Update is called once per frame
    void Update()
    {
    }

    public List<Boid> GetFlock()
    {
        return flock;
    }

    public void RemoveFromFlock(Selectable selectable)
    {
        selectable.GetComponent<Boid>().Die();
        flock.Remove(selectable.gameObject.GetComponent<Boid>());
    }

    public void AddBoins(int boinsToAdd)
    {
        boins += boinsToAdd;
    }

    public bool RemoveBoins(int boinsToRemove)
    {
        bool isSuccess;
        if (isSuccess = boinsToRemove <= boins)
        {
            boins -= boinsToRemove;
        }
        return isSuccess;
    }

    public int GetBoins()
    {
        return boins;
    }

    public void AddScore(int pointsToAdd)
    {
        score += pointsToAdd;
    }

    public string GetNickname()
    {
        return nickname;
    }

    public SpawnArea GetSpawnArea()
    {
        return GetComponentInChildren<SpawnArea>();
    }

    public GameUI GetGameUI()
    {
        return _gameUI;
    }

    public bool CanReady()
    {
        foreach (Boid boid in flock)
        {
            if (boid.GetType() == Boid.Type.Hero || boid.GetType() == Boid.Type.Melee || boid.GetType() == Boid.Type.Ranged)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsReady()
    {
        return ready;
    }

    public void Ready()
    {
        if (CanReady())
        {
            ready = true;
        }
    }

    public void Unready()
    {
        ready = false;
    }

    public FlockInfo GetFlockInfo()
    {
        return _flockInfo;
    }

    public void SetFlockInfo(FlockInfo flockInfo)
    {
        this._flockInfo = flockInfo;
    }

    public void SetActive(bool active)
    {
        _active = active;
        var state = GetComponentInParent<GameManager>().GetState();
        var type = GetComponentInParent<GameManager>().GetType();
        if (_active && state == GameManager.GameState.Placement) {
            GetComponentInChildren<SpawnArea>().Activate();
        } else {
            GetComponentInChildren<SpawnArea>().Deactivate();
        }
        foreach (var boid in flock) {
            boid.SetHidden(!_active && state == GameManager.GameState.Placement && type == SceneData.Type.Multiplayer);
        }
    }

    public void SetId(int id) {
        this.id = id;
    }

    public void SetNickname(string nickname) {
        this.nickname = nickname;
    }

    public void SetColor(Color color) {
        this.color = color;
    }

    public void SetBoins(int boins) {
        this.boins = boins;
    }
}
