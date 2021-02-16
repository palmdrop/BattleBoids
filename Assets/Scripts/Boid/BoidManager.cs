using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;

public class BoidManager : MonoBehaviour
{
    [SerializeField] private List<Player> players = new List<Player>();

    // To be replaced by some other data structure
    private List<Boid> _boids = new List<Boid>();

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // When game is started, clear boids and fetch all the new boids from the 
        // corresponding players
        if (Input.GetKey("p")) {
            _boids.Clear();
            foreach (Player p in players) {
                foreach (GameObject b in p.GetFlock()) {
                    _boids.Add(b.GetComponent<Boid>());
                }
            }
        }

        // Remove all dead boids
        _boids.RemoveAll(b => b.dead);

        // Allocate arrays for all the data required to calculate the boid behaviors
        // In data
        NativeArray<Boid.BoidInfo> boidInfos = new NativeArray<Boid.BoidInfo>(_boids.Count, Allocator.TempJob);
        
        // Out data 
        NativeArray<float3> forces = new NativeArray<float3>(_boids.Count, Allocator.TempJob);
        NativeArray<Player.FlockInfo> flockInfos = new NativeArray<Player.FlockInfo>(players.Count, Allocator.TempJob);

        // Get all the boid info from the boids
        for (int i = 0; i < _boids.Count; i++)
        {
            boidInfos[i] = _boids[i].GetInfo();
        }
        
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
        BoidStructJob boidJob = new BoidStructJob
        {
            flocks = flockInfos,
            boids = boidInfos,
            forces = forces,
        };

        // Schedule job 
        jobHandle = boidJob.Schedule(_boids.Count, _boids.Count / 10);
        jobHandle.Complete();

        // Update all the data 
        for (int i = 0; i < flockInfos.Length; i++)
        {
            players[i].SetFlockInfo(flockInfos[i]);
        }
        
        for (int i = 0; i < _boids.Count; i++)
        {
            _boids[i].UpdateBoid(forces[i]);
        }

        // Dispose of all data
        //flockInfos.Dispose();
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
            NativeArray<Player.FlockInfo> tempFlockInfos = new NativeArray<Player.FlockInfo>(flockInfos.Length, Allocator.Temp);
            
            for (int i = 0; i < flockInfos.Length; i++)
            {
                tempFlockInfos[i] = new Player.FlockInfo();
            }

            for (int i = 0; i < boids.Length; i++)
            {
                Boid.BoidInfo boid = boids[i];
                Player.FlockInfo flockInfo = tempFlockInfos[boid.flockId - 1];
                flockInfo.avgPos += boid.pos;
                flockInfo.avgVel += boid.vel;
                flockInfo.boidCount++;

                tempFlockInfos[boid.flockId - 1] = flockInfo;
            }


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

            tempFlockInfos.Dispose();
        }
    }

    // This job calculates all the forces acting on the boids
    [BurstCompile]
    public struct BoidStructJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Player.FlockInfo> flocks;
        [ReadOnly] public NativeArray<Boid.BoidInfo> boids;
        [WriteOnly] public NativeArray<float3> forces;

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
                    if (sqrDist < boid.classInfo.separationRadius * boid.classInfo.separationRadius)
                    {
                        // Add to average velocity
                        avgVel += boids[i].vel;
                        viewCount++;

                        // Add to average position for cohesion
                        avgPosCohesion += boids[i].pos;

                        avgPosSeparation += boids[i].pos;
                        separationViewCount++;
                    }
                    else if (sqrDist < boid.classInfo.viewRadius * boid.classInfo.viewRadius)
                    {
                        // Add to average velocity
                        avgVel += boids[i].vel;
                        viewCount++;

                        // Add to average position for cohesion
                        avgPosCohesion += boids[i].vel;

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
            //if (targetDist == Mathf.Infinity) aggressionForce = new float3(0, 0, 0);
            if (enemyFlock.boidCount == 0) aggressionForce = new float3(0, 0, 0);
            else
                aggressionForce =
                    //math.normalize(targetPos - boid.pos) * boid.classInfo.aggressionStrength * targetDist;
                    math.normalize(enemyFlockPos - boid.pos) * boid.classInfo.aggressionStrength;
            
            // Calculate fear force
            Vector3 fearForce;
            if (targetDist == math.INFINITY) fearForce = new float3(0, 0, 0);
            else
                fearForce = math.normalize(boid.pos - enemyFlockPos) * boid.classInfo.fearStrength *
                            math.pow(targetDist, boid.classInfo.fearExponent);

            forces[index] = 
                        alignmentForce 
                            + cohesionForce 
                            + separationForce
                            + aggressionForce
                            + fearForce
            ;
        }
    }
}
