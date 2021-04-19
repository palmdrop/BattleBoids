using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PerformanceMap : MonoBehaviour
{
    [Header("Change these")]
    public Boid boidToTest;
    [SerializeField] private int player1BoidAmount;
    [SerializeField] private int player2BoidAmount;

    [Header("Don't change these")]
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private Player player1;
    [SerializeField] private Player player2;
    [SerializeField] private Transform player1SpawningLocation;
    [SerializeField] private Transform player1SpawningStop;
    [SerializeField] private Transform player2SpawningLocation;
    [SerializeField] private Transform player2SpawningStop;

    // Start is called before the first frame update
    void Start()
    {
        float offset = 0.5f;

        List<Boid> player1Flock = player1.GetFlock();
        float x = player1SpawningLocation.position.x;
        float y = player1SpawningLocation.position.y;
        float z = player1SpawningLocation.position.z;
        int amountOnThisRow = 0;
        for (int i = 0; i < player1BoidAmount; i++)
        {
            if (x + offset > player1SpawningStop.position.x)
            {
                x = player1SpawningLocation.position.x;
                z += offset;
                amountOnThisRow = 0;
            }
            Boid boid = Instantiate(boidToTest, new Vector3(x, y, z), Quaternion.identity, player1.gameObject.transform);
            boid.SetOwner(player1);
            player1Flock.Add(boid);
            x += offset;
            amountOnThisRow++;
        }


        List<Boid> player2Flock = player2.GetFlock();
        x = player2SpawningLocation.position.x;
        y = player2SpawningLocation.position.y;
        z = player2SpawningLocation.position.z;
        amountOnThisRow = 0;
        for (int i = 0; i < player2BoidAmount; i++)
        {
            if (x + offset > player2SpawningStop.position.x)
            {
                x = player2SpawningLocation.position.x;
                z += offset;
                amountOnThisRow = 0;
            }
            Boid boid = Instantiate(boidToTest, new Vector3(x, y, z), Quaternion.identity, player2.gameObject.transform);
            boid.SetOwner(player2);
            player2Flock.Add(boid);
            x += offset;
            amountOnThisRow++;
        }
    }
}
