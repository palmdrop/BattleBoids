using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using Random = System.Random;

public class BoidManager : MonoBehaviour
{
    [SerializeField] private List<Player> players = new List<Player>();
    [SerializeField] private GameUI canvas;

    // To be replaced by some other data structure
    private List<Boid> _boids = new List<Boid>();
    
    // Random number generator
    private Random random = new Random();

    // Start is called before the first frame update
    void Start()
    {
    }

    // Fetches the boids from the respective players and places them in the boids list
    private void AddPlayerBoids()
    {
        canvas.GetReadyButton().gameObject.SetActive(false);
        _boids.Clear();
        foreach (Player p in players)
        {
            foreach (GameObject b in p.GetFlock())
            {
                _boids.Add(b.GetComponent<Boid>());
            }
        }
    }

    // Clear dead boids
    private void ClearDeadBoids()
    {
        _boids.RemoveAll(b => b.dead);
    }

    // Update is called once per frame
    void Update()
    {
        // When game is started, clear boids and fetch all the new boids from the 
        // corresponding players
        if (canvas.AllPlayersReady())
        {
            AddPlayerBoids();
        }

        // Remove all dead boids
        ClearDeadBoids();

        // Allocate arrays for all the data required to calculate the boid behaviors
        NativeArray<Boid.BoidInfo> boidInfos = new NativeArray<Boid.BoidInfo>(_boids.Count, Allocator.TempJob); // In data
        
        // Get all the boid info from the boids
        for (int i = 0; i < _boids.Count; i++)
        {
            boidInfos[i] = _boids[i].GetInfo();
        }
        
        // A force is calculated for each boid and then later applied 
        NativeArray<float3> forces = new NativeArray<float3>(_boids.Count, Allocator.TempJob); // Out data
        // Information about the entire flocks is gathered
        NativeArray<Player.FlockInfo> flockInfos = new NativeArray<Player.FlockInfo>(players.Count, Allocator.TempJob); // Out and in data
        
        // Allocate a struct job for calculating flock info
        FlockStructJob flockJob = new FlockStructJob()
        {
            boids = boidInfos,
            flockInfos = flockInfos
        };
        
        // Schedule job 
        JobHandle jobHandle = flockJob.Schedule();
        jobHandle.Complete();

        // Allocate a struct job for calculating boid forces
        NativeArray<float> randomFloats = new NativeArray<float>(_boids.Count * 2, Allocator.TempJob);

        for (int i = 0; i < randomFloats.Length; i++)
        {
            randomFloats[i] = (float)random.NextDouble();
        }
        
        BoidStructJob boidJob = new BoidStructJob
        {
            random = randomFloats,
            flocks = flockInfos,
            boids = boidInfos,
            forces = forces,
        };

        // Schedule job 
        jobHandle = boidJob.Schedule(_boids.Count, _boids.Count / 10);
        jobHandle.Complete();

        // Update player data with new flock info
        for (int i = 0; i < flockInfos.Length; i++)
        {
            players[i].SetFlockInfo(flockInfos[i]);
        }
        
        // Update boids using the calculated forces
        for (int i = 0; i < _boids.Count; i++)
        {
            _boids[i].UpdateBoid(forces[i]);
        }

        // Dispose of all data
        randomFloats.Dispose();
        boidInfos.Dispose();
        forces.Dispose();
        flockInfos.Dispose();
    }

    // This job calculates information specific for the entire flock, such as 
    // average velocity, average position and entity count
    [BurstCompile]
    public struct FlockStructJob : IJob
    {
        [ReadOnly] public NativeArray<Boid.BoidInfo> boids;
        [WriteOnly] public NativeArray<Player.FlockInfo> flockInfos;

        public void Execute()
        {
            // Temporary array for holding flock info data
            NativeArray<Player.FlockInfo> tempFlockInfos = new NativeArray<Player.FlockInfo>(flockInfos.Length, Allocator.Temp);
            
            // Iterate over all of the boids in order to calculate flock info data
            for (int i = 0; i < boids.Length; i++)
            {
                Boid.BoidInfo boid = boids[i];
                
                // Translate the flockid to an index in the array
                Player.FlockInfo flockInfo = tempFlockInfos[boid.flockId - 1];
                
                // Contribute to flock data
                flockInfo.avgPos += boid.pos;
                flockInfo.avgVel += boid.vel;
                flockInfo.boidCount++;

                // Save new data in array (necessary since "flockInfo" is a temporary value)
                tempFlockInfos[boid.flockId - 1] = flockInfo;
            }

            // Iterate over all the temporary flock info structs and average the results
            // Also assign the data to the output array
            for (int i = 0; i < tempFlockInfos.Length; i++)
            {
                Player.FlockInfo flockInfo = tempFlockInfos[i];
                if (flockInfo.boidCount != 0)
                {
                    flockInfo.avgPos /= flockInfo.boidCount;
                    flockInfo.avgVel /= flockInfo.boidCount;
                }

                flockInfos[i] = flockInfo;
            }

            // Dispose of temporary data
            tempFlockInfos.Dispose();
        }
    }

    // This job calculates all the forces acting on the boids
    [BurstCompile]
    public struct BoidStructJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> random;
        [ReadOnly] public NativeArray<Player.FlockInfo> flocks;
        [ReadOnly] public NativeArray<Boid.BoidInfo> boids;
        [WriteOnly] public NativeArray<float3> forces;

        private float3 CalculateForce(Vector3 dir, float weight)
        {
            return CalculateForce(dir, weight, 0, 1, 0);
        }

        private float3 CalculateForce(Vector3 dir, float weight, float dist, float maxDist, float exponent)
        {
            return math.normalize(dir) * weight * math.pow((dist / maxDist), exponent);
        }

        public void Execute(int index)
        {
            /*** BOID BEHAVIOR VARIABLES ***/
            // Average velocity is used to calculate alignment force
            float3 avgVel = new Vector3(0, 0, 0);

            // Average neighbour position used to calculate cohesion
            float3 avgPosCohesion = new Vector3(0, 0, 0);

            // Average neighbour position used to calculate cohesion
            float3 avgPosSeparation = new Vector3(0, 0, 0);

            // Position of closest enemy
            float3 targetPos = new Vector3(0, 0, 0);
            float targetDist = Mathf.Infinity;

            Boid.BoidInfo boid = boids[index];

            // Iterate over all the neighbours
            int viewCount = 0;
            int separationViewCount = 0;
            for (int i = 0; i < boids.Length; i++)
            {
                if (i == index) continue;

                // Compare the distance between this boid and the neighbour using the
                // square of the distance and radius. This avoids costly square root operations
                // And if close enough, add to average position for separation
                float3 vector = (boid.pos - boids[i].pos);
                float sqrDist = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;

                if (boids[i].flockId == boid.flockId) {
                    // Friendly boid
                    
                    // If friendly boid is within viewRadius...
                    if (sqrDist < boid.classInfo.viewRadius * boid.classInfo.viewRadius)
                    {
                        // Add to average velocity
                        avgVel += boids[i].vel;
                        viewCount++;

                        // Add to average position for cohesion
                        avgPosCohesion += boids[i].pos;
                    }
                    
                    // If friendly boid is within separationRadius...
                    if (sqrDist < boid.classInfo.separationRadius * boid.classInfo.separationRadius)
                    {
                        // Add to average position for separation
                        avgPosSeparation += boids[i].pos;
                        separationViewCount++;
                    }
                } else {
                    // Enemy boid
                    if (sqrDist < boid.classInfo.viewRadius * boid.classInfo.viewRadius && sqrDist < targetDist) {
                        targetPos = boids[i].pos;
                        targetDist = sqrDist;
                    }
                }
            }

            // Calculate alignment force
            Vector3 alignmentForce;
            if (viewCount == 0 || avgVel.Equals(new float3(0, 0, 0))) alignmentForce = new float3(0, 0, 0);
            else alignmentForce = math.normalize(avgVel) * boid.classInfo.alignmentStrength;

            // Calculate cohesion force
            Vector3 cohesionForce;
            if (viewCount == 0) cohesionForce = new float3(0, 0, 0);
            else cohesionForce = math.normalize((avgPosCohesion / viewCount) - boid.pos) * boid.classInfo.cohesionStrength;

            // Calculate separation force
            Vector3 separationForce;
            if (separationViewCount == 0) separationForce = new float3(0, 0, 0);
            else separationForce = math.normalize(boid.pos - (avgPosSeparation / separationViewCount)) * boid.classInfo.separationStrength;

            // Calculate aggression force
            Player.FlockInfo enemyFlock = flocks[boid.flockId == 1 ? 1 : 0];
            float3 enemyFlockPos = enemyFlock.avgPos;
            Vector3 aggressionForce;
            
            if (enemyFlock.boidCount == 0) aggressionForce = new float3(0, 0, 0);
            else
                aggressionForce =
                    math.normalize(enemyFlockPos - boid.pos) * boid.classInfo.aggressionStrength;
            
            // Calculate fear force
            // The strength of the force is calculated using the closest enemy
            // TODO use closeness of entire enemy flock instead?
            Vector3 fearForce;
            if (targetDist == math.INFINITY) fearForce = new float3(0, 0, 0);
            else
                fearForce = math.normalize(boid.pos - enemyFlockPos) * boid.classInfo.fearStrength *
                            math.pow(targetDist, boid.classInfo.fearExponent);
            
            
            // Calculate random force
            float angle = math.PI * 2 * random[index * 2];
            float size = random[index * 2 + 1];
            Vector3 randomForce = new float3(math.cos(angle), 0.0f, math.sin(angle)) * size *
                                  boid.classInfo.randomMovements;

            // Sum all the forces
            forces[index] = 
                        alignmentForce 
                            + cohesionForce 
                            + separationForce
                            + aggressionForce
                            + fearForce
                            + randomForce
            ;
        }
    }
}
