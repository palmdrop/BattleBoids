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
        if (Input.GetKey("p")) {
            _boids.Clear();
            foreach (Player p in players) {
                foreach (GameObject b in p.GetFlock()) {
                    _boids.Add(b.GetComponent<Boid>());
                }
            }
        }

        _boids.RemoveAll(b => b.dead);

        NativeArray<Boid.BoidInfo> boidInfos = new NativeArray<Boid.BoidInfo>(_boids.Count, Allocator.TempJob);
        NativeArray<float3> forces = new NativeArray<float3>(_boids.Count, Allocator.TempJob);

        for (int i = 0; i < _boids.Count; i++)
        {
            boidInfos[i] = _boids[i].GetInfo();
        }

        BoidStructJob boidJob = new BoidStructJob
            {
                boids = boidInfos,
                forces = forces
            };

        JobHandle jobHandle = boidJob.Schedule(_boids.Count, _boids.Count / 10);
        jobHandle.Complete();

        for (int i = 0; i < _boids.Count; i++)
        {
            _boids[i].UpdateBoid(forces[i]);
        }

        boidInfos.Dispose();
        forces.Dispose();
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

    [BurstCompile]
    public struct BoidStructJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Boid.BoidInfo> boids;
        [WriteOnly] public NativeArray<float3> forces;

        public void Execute(int index)
        {
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
            Vector3 aggressionForce;
            if (targetDist == Mathf.Infinity) aggressionForce = new float3(0, 0, 0);
            else aggressionForce = math.normalize(targetPos - boid.pos) * boid.classInfo.aggressionStrength * targetDist;


            forces[index] = alignmentForce + cohesionForce + separationForce + aggressionForce;
        }
    }
}
