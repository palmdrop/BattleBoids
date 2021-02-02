using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidManager : MonoBehaviour
{
    // To be replaced by some other data structure
    public Boid[] boids;

    // Start is called before the first frame update
    void Start()
    {
        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("Boid");
        boids = new Boid[gameObjects.Length];
        for (int i = 0; i < boids.Length; i++)
        {
            boids[i] = gameObjects[i].GetComponent<Boid>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Here we will build or update the data structure that we will use for efficiently finding boids within some radius

        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("Boid");
        boids = new Boid[gameObjects.Length];
        for (int i = 0; i < boids.Length; i++)
        {
            boids[i] = gameObjects[i].GetComponent<Boid>();
        }

        foreach (Boid b in boids)
        {
            b.UpdateBoid();
        }
    }

    // Finds all boids within the given radius from the given boid (excludes the given boid itself)
    public Boid[] findBoidsWithinRadius(Boid boid, float radius)
    {
        // For now, just loop over all boids and check distance
        // In the future, make use of an efficient data structure
        List<Boid> result = new List<Boid>();
        foreach (Boid b in boids)
        {
            if ((b.transform.position - boid.transform.position).magnitude < radius && b != boid)
            {
                result.Add(b);
                //Debug.Log("In range: " + (b.transform.position - boid.transform.position).magnitude);
            } /*else
            {
                Debug.Log("Not in range");
            }*/
        }
        return result.ToArray();
    }
}
