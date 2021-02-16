using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Player : MonoBehaviour
{
    public struct FlockInfo
    {
        public float3 avgPos;
        public float3 avgVel;
        public int boidCount;
    }
    
    [SerializeField] private List<GameObject> flock = new List<GameObject>();
    [SerializeField] private int boins; // Currency to buy boids
    [SerializeField] private int score; // Points obtained during a match
    [SerializeField] public int id; // Identifier
    [SerializeField] public Color color; // Player color

    private FlockInfo _flockInfo;

    // Start is called before the first frame update
    void Start()
    {
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

/* Future methods, no Boid class exists yet
    public bool buyBoid(Boid boid)
    {
        int cost = boid.GetCost();
        bool canAfford;
        if (canAfford = boins >= cost)
        {
            boins -= cost;
        }
        return canAfford;
    }

    public bool sellBoid(Boid boid)
    {
        bool ownsBoid;
        if (ownsBoid = flock.Contains(boid))
        {
            boins += boid.GetCost();
        }
        return ownsBoid;
    }
*/

    public void AddScore(int pointsToAdd)
    {
        score += pointsToAdd;
    }

    public List<GameObject> GetFlock()
    {
        return flock;
    }

    public FlockInfo GetFlockInfo()
    {
        return _flockInfo;
    }

    public void SetFlockInfo(FlockInfo flockInfo)
    {
        this._flockInfo = flockInfo;
    }
}
