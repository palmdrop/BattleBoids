using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidManager : MonoBehaviour
{
    // To be replaced by some other data structure
    private Boid[] _boids;

    // Start is called before the first frame update
    void Start()
    {
        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("Boid");
        _boids = new Boid[gameObjects.Length];
        for (int i = 0; i < _boids.Length; i++)
        {
            _boids[i] = gameObjects[i].GetComponent<Boid>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Here we will build or update the data structure that we will use for efficiently finding boids within some radius

        foreach (Boid b in _boids)
        {
            b.UpdateBoid();
        }
    }

    // Finds all boids within the given radius from the given boid (excludes the given boid itself)
    public Boid[] FindBoidsWithinRadius(Boid boid, float radius)
    {
        // For now, just loop over all boids and check distance
        // In the future, make use of an efficient data structure
        List<Boid> result = new List<Boid>();
        foreach (Boid b in _boids)
        {
            if ((b.GetPos() - boid.GetPos()).sqrMagnitude < (radius * radius) && b != boid)
            {
                result.Add(b);
            }
        }
        return result.ToArray();
    }

    public Boid[] getBoids()
    {
        return _boids;
    }
}
