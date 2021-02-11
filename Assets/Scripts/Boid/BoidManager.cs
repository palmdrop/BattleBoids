using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
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
        NativeArray<Boid.BoidInfo> boidInfos = new NativeArray<Boid.BoidInfo>(_boids.Length, Allocator.TempJob);
        NativeArray<float3> forces = new NativeArray<float3>(_boids.Length, Allocator.TempJob);

        for (int i = 0; i < _boids.Length; i++)
        {
            boidInfos[i] = _boids[i].GetInfo();
        }

        BoidStructJob boidJob = new BoidStructJob
            {
                boids = boidInfos,
                forces = forces
            };

        JobHandle jobHandle = boidJob.Schedule(_boids.Length, _boids.Length / 10);
        jobHandle.Complete();

        for (int i = 0; i < _boids.Length; i++)
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


            forces[index] = alignmentForce + cohesionForce + separationForce;
        }
    }
}
