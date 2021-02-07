using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;

public class BoidManager : MonoBehaviour
{
    // To be replaced by some other data structure
    private Boid[] _boids;

    private bool _usingJobs = false;

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

        if (_usingJobs) 
        {
           
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

    public struct BoidStructJob : IJobParallelFor
    {
        public NativeArray<float3> vel;
        public NativeArray<float3> pos;
        public NativeArray<float3> force;
        public NativeArray<float> separationRadius;
        public NativeArray<float> viewRadius;
        public NativeArray<float> alignmentStrength;
        public NativeArray<float> cohesionStrength;
        public NativeArray<float> separationStrength;

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
            if (viewCount == 0) alignmentForce = new float3(0, 0, 0);
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
