using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

public class BoidManager : MonoBehaviour
{
    // To be replaced by some other data structure
    private Boid[] _boids;

    public bool usingBurst = true;

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

        if (usingBurst) 
        {
            NativeArray<float3> posArray = new NativeArray<float3>(_boids.Length, Allocator.TempJob);
            NativeArray<float3> velArray = new NativeArray<float3>(_boids.Length, Allocator.TempJob);
            NativeArray<float3> forceArray = new NativeArray<float3>(_boids.Length, Allocator.TempJob);
            NativeArray<float> separationRadiusArray = new NativeArray<float>(_boids.Length, Allocator.TempJob);
            NativeArray<float> viewRadiusArray = new NativeArray<float>(_boids.Length, Allocator.TempJob);
            NativeArray<float> alignmentStrengthArray = new NativeArray<float>(_boids.Length, Allocator.TempJob);
            NativeArray<float> cohesionStrengthArray = new NativeArray<float>(_boids.Length, Allocator.TempJob);
            NativeArray<float> separationStrengthArray = new NativeArray<float>(_boids.Length, Allocator.TempJob);

            for (int i = 0; i < _boids.Length; i++)
            {
                posArray[i] = _boids[i].GetPos();
                velArray[i] = _boids[i].GetVel();
                forceArray[i] = new float3(1,1,1);
                separationRadiusArray[i] = _boids[i].GetSeparationRadius();
                viewRadiusArray[i] = _boids[i].GetViewRadius();
                alignmentStrengthArray[i] = _boids[i].GetAlignmentStrength();
                cohesionStrengthArray[i] = _boids[i].GetCohesionStrength();
                separationStrengthArray[i] = _boids[i].GetSeparationStrength();
            }

            BoidStructJob boidJob = new BoidStructJob
            {
                vel = velArray,
                pos = posArray,
                force = forceArray,
                separationRadius = separationRadiusArray,
                viewRadius = viewRadiusArray,
                alignmentStrength = alignmentStrengthArray,
                cohesionStrength = cohesionStrengthArray,
                separationStrength = separationStrengthArray

            };


            JobHandle jobHandle = boidJob.Schedule(_boids.Length, _boids.Length / 10);
            jobHandle.Complete();

            for (int i = 0; i < _boids.Length; i++)
            {
                _boids[i].AddForce(forceArray[i]);
                _boids[i].UpdateDirection();
            }
            posArray.Dispose();
            velArray.Dispose();
            forceArray.Dispose();
            separationRadiusArray.Dispose();
            viewRadiusArray.Dispose();
            alignmentStrengthArray.Dispose();
            cohesionStrengthArray.Dispose();
            separationStrengthArray.Dispose();
        }
        else
        {
            foreach (Boid b in _boids)
            {
                b.UpdateBoid();
            }
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

    [BurstCompile]
    public struct BoidStructJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> vel;
        [ReadOnly] public NativeArray<float3> pos;
        public NativeArray<float3> force;
        [ReadOnly] public NativeArray<float> separationRadius;
        [ReadOnly] public NativeArray<float> viewRadius;
        [ReadOnly] public NativeArray<float> alignmentStrength;
        [ReadOnly] public NativeArray<float> cohesionStrength;
        [ReadOnly] public NativeArray<float> separationStrength;

        public void Execute(int index)
        {
            // Average velocity is used to calculate alignment force
            float3 avgVel = new Vector3(0, 0, 0);

            // Average neighbour position used to calculate cohesion
            float3 avgPosCohesion = new Vector3(0, 0, 0);

            // Average neighbour position used to calculate cohesion
            float3 avgPosSeparation = new Vector3(0, 0, 0);

            // Iterate over all the neighbours
            int viewCount = 0;
            int separationViewCount = 0;
            for (int i = 0; i < pos.Length; i++)
            {
                if (i == index) continue;

                // Compare the distance between this boid and the neighbour using the
                // square of the distance and radius. This avoids costly square root operations
                // And if close enough, add to average position for separation
                float3 vector = (pos[index] - pos[i]);
                float sqrDist = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;

                if (sqrDist < separationRadius[index] * separationRadius[index])
                {
                    // Add to average velocity
                    avgVel += vel[i];
                    viewCount++;

                    // Add to average position for cohesion
                    avgPosCohesion += pos[i];

                    avgPosSeparation += pos[i];
                    separationViewCount++;
                }
                else if (sqrDist < viewRadius[index] * viewRadius[index])
                {
                    // Add to average velocity
                    avgVel += vel[i];
                    viewCount++;

                    // Add to average position for cohesion
                    avgPosCohesion += vel[i];

                }
            }

            // Calculate alignment force
            Vector3 alignmentForce;
            if (viewCount == 0 || avgVel.Equals(new float3(0,0,0))) alignmentForce = new float3(0, 0, 0);
            else alignmentForce = math.normalize(avgVel) * alignmentStrength[index];

            // Calculate cohesion force
            Vector3 cohesionForce;
            if (viewCount == 0) cohesionForce = new float3(0, 0, 0);
            else cohesionForce = math.normalize((avgPosCohesion / viewCount) - pos[index]) * cohesionStrength[index];

            // Calculate separation force
            Vector3 separationForce;
            if (separationViewCount == 0) separationForce = new float3(0, 0, 0);
            else separationForce = math.normalize(pos[index] - (avgPosSeparation / separationViewCount)) * separationStrength[index];


            force[index] = alignmentForce + cohesionForce + separationForce;

            //_rigidbody.AddForce(force, ForceMode.Acceleration);
            //transform.forward = _rigidbody.velocity;
        }

    }
}
