using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class PerformanceMap : MonoBehaviour
{
    [Header("Change these")]
    public Boid boidToTest;
    [SerializeField] private int player1BoidAmount;
    [SerializeField] private int player2BoidAmount;
    [SerializeField] private float timeToTest;

    [Header("Don't change these")]
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private Player player1;
    [SerializeField] private Player player2;
    [SerializeField] private Transform player1SpawningLocation;
    [SerializeField] private Transform player1SpawningStop;
    [SerializeField] private Transform player2SpawningLocation;
    [SerializeField] private Transform player2SpawningStop;

    private List<float> frameTimes = new List<float>();
    private float startTime = Time.time;
    private bool hasLogged = false;

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
        player1.Ready();


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
        player2.Ready();
    }


    void Update()
    {
        if (Time.time - startTime <= timeToTest)
        {
            frameTimes.Add(Time.deltaTime);
        }

        if (Time.time - startTime > timeToTest && !hasLogged)
        {
            // create the log
            // save the log
            string result = "";
            float sum = 0;
            for (int i = 0; i < frameTimes.Count - 1; i++)
            {
                result += frameTimes[i].ToString().Replace(",", ".") + ",";
                sum += frameTimes[i];
            }
            result += frameTimes[frameTimes.Count - 1];
            sum += frameTimes[frameTimes.Count - 1];
            float average = sum / frameTimes.Count;
            print("Average frame time: " + average);
            print("Average fps: " + (1f / average));

            //print(result);

            //float sum = 0;

            //foreach (float frameTime in frameTimes)
            //{
            //    sum += frameTime;
            //}

            //float average = sum / frameTimes.Count;

            //print("Average frame time: " + average);
            //print("Average fps: " + (1f / average));

            string path = "Assets/Resources/PerformanceLogs/performanceLog.txt";
            StreamWriter writer = new StreamWriter(path, true);
            writer.WriteLine(result);
            writer.Close();

            print("Logging complete");

            hasLogged = true;
        }
    }
}
