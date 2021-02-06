using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidSpawner : MonoBehaviour
{
    [SerializeField] private Boid boidPrefab;
    [SerializeField] private float spawnRadius = 1f;
    [SerializeField] private int spawnCount = 10;

    [SerializeField] private BoidManager manager;

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Boid boid = Instantiate(boidPrefab, manager.transform);
            Transform boidTransform = boid.transform;
            
            Vector2 randomPos = Random.insideUnitCircle;
            Vector3 pos = transform.position + new Vector3(randomPos.x, 0, randomPos.y) * spawnRadius;
            boidTransform.position = pos;
            
            Vector2 randomHeading = Random.insideUnitCircle;
            boidTransform.forward = new Vector3(randomHeading.x, 0, randomHeading.y);
        }
    }

}
