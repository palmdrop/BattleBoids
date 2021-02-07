using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;

public class Boid : MonoBehaviour
{
    [SerializeField] private float viewRadius = 5f;
    [SerializeField] private float separationRadius = 1f;
    [SerializeField] private float alignmentStrength = 1.1f;
    [SerializeField] private float cohesionStrength = 1.2f;
    [SerializeField] private float separationStrength = 3f;
    
    private BoidManager _manager;
    private Rigidbody _rigidbody;

    // Start is called before the first frame update
    void Start()
    {
        _manager = gameObject.GetComponentInParent<BoidManager>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    // Called by the boid manager
    // Updates the boid according to the standard flocking behaviour
    public void UpdateBoid()
    {
        //Boid[] neighbours = _manager.FindBoidsWithinRadius(this, viewRadius);
        Boid[] neighbors = _manager.getBoids();
        Vector3 force = CalculateSteeringForce(neighbors);

        _rigidbody.AddForce(force, ForceMode.Acceleration);
        transform.forward = _rigidbody.velocity;
    }

    public void AddForce(Vector3 force)
    {
       _rigidbody.AddForce(force, ForceMode.Acceleration);
    }

    public void UpdateDirection()
    {
        transform.forward = _rigidbody.velocity;
    }

    // Returns the position of this boid
    public Vector3 GetPos()
    {
        return _rigidbody.position;
    }

    // Returns the velocity of this boid
    public Vector3 GetVel()
    {
        return _rigidbody.velocity;
    }
    public float GetViewRadius()
    {
        return viewRadius;
    }
    public float GetSeparationRadius()
    {
        return separationRadius;
    }
    public float GetAlignmentStrength()
    {
        return alignmentStrength;
    }
    public float GetCohesionStrength()
    {
        return cohesionStrength;
    }
    public float GetSeparationStrength()
    {
        return separationStrength;
    }

    private Vector3 CalculateSteeringForce(Boid[] neighbours)
    {
        // Average velocity is used to calculate alignment force
        Vector3 avgVel = new Vector3(0, 0, 0);
        
        // Average neighbour position used to calculate cohesion
        Vector3 avgPosCohesion = new Vector3(0, 0, 0);
        
        // Average neighbour position used to calculate cohesion
        Vector3 avgPosSeparation = new Vector3(0, 0, 0);

        // Iterate over all the neighbours
        int viewCount = 0;
        int separationViewCount = 0;
        foreach (Boid b in neighbours)
        {

            // Compare the distance between this boid and the neighbour using the
            // square of the distance and radius. This avoids costly square root operations
            // And if close enough, add to average position for separation
            float sqrDist = (this.GetPos() - b.GetPos()).sqrMagnitude;

            if (sqrDist < separationRadius * separationRadius)
            {
                // Add to average velocity
                avgVel += b.GetVel();
                viewCount++;

                // Add to average position for cohesion
                avgPosCohesion += b.GetPos();

                avgPosSeparation += b.GetPos();
                separationViewCount++;
            }
            else if (sqrDist < viewRadius * viewRadius)
            {
                // Add to average velocity
                avgVel += b.GetVel();
                viewCount++;

                // Add to average position for cohesion
                avgPosCohesion += b.GetPos();

            }
        }
            

        // Calculate alignment force
        Vector3 alignmentForce;
        if (viewCount == 0) alignmentForce = new Vector3(0, 0, 0);
        else alignmentForce = avgVel.normalized * alignmentStrength;
        
        // Calculate cohesion force
        Vector3 cohesionForce;
        if (viewCount == 0) cohesionForce = new Vector3(0, 0, 0);
        else cohesionForce = ((avgPosCohesion / viewCount) - GetPos()).normalized * cohesionStrength;
        
        // Calculate separation force
        Vector3 separationForce;
        if (separationViewCount == 0) separationForce = new Vector3(0, 0, 0);
        else separationForce = (GetPos() - (avgPosSeparation / separationViewCount)).normalized * separationStrength;

        return alignmentForce + cohesionForce + separationForce;
    }


    
}
