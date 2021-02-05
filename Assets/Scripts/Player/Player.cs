using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private List<GameObject> flock = new List<GameObject>();
    [SerializeField] private int boins; // Currency to buy boids
    [SerializeField] private int score; // Points obtained during a match

    // Start is called before the first frame update
    void Start()
    {
        
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
}
