using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidSpawner : MonoBehaviour
{

    public Boid boidPrefab;
    public float spawnRadius = 1f;
    public int spawnCount = 10;

    private void Awake()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 randomPos = Random.insideUnitCircle;
            Vector3 pos = transform.position + new Vector3(randomPos.x, 0, randomPos.y) * spawnRadius;
            Boid boid = Instantiate(boidPrefab);
            boid.transform.position = pos;
            Vector2 randomHeading = Random.insideUnitCircle;
            boid.transform.forward = new Vector3(randomHeading.x, 0, randomHeading.y);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

}
