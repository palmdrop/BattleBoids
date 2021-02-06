using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{

    [SerializeField] private float viewRadius = 5f;
    [SerializeField] private float separationRadius = 2f;
    [SerializeField] private float alignmentStrength = 0.1f;
    [SerializeField] private float cohesionStrength = 0.2f;
    [SerializeField] private float separationStrength = 3f;
    
    private BoidManager _manager;
    private Rigidbody _rigidbody;

    // Start is called before the first frame update
    void Start()
    {
        _manager = GameObject.FindGameObjectWithTag("BoidManager").GetComponent<BoidManager>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    // Called by the boid manager
    // Updates the boid according to the standard flocking behaviour
    public void UpdateBoid()
    {
        Boid[] neighbours = _manager.findBoidsWithinRadius(this, viewRadius);
        
        Vector3 alignmentForce = alignment(neighbours) * alignmentStrength;
        Vector3 cohesionForce = cohesion(neighbours) * cohesionStrength;

        neighbours = _manager.findBoidsWithinRadius(this, separationRadius);
        Vector3 separationForce = separation(neighbours) * separationStrength;

        Vector3 force = alignmentForce + cohesionForce + separationForce;

        _rigidbody.AddForce(force, ForceMode.Acceleration);

        transform.forward = _rigidbody.velocity;
    }

    // Returns the position of this boid
    public Vector3 getPos()
    {
        return _rigidbody.position;
    }

    // Returns the velocity of this boid
    public Vector3 getVel()
    {
        return _rigidbody.velocity;
    }

    // Returns a direction vector for the alignment part of the flocking behaviour
    private Vector3 alignment(Boid[] neighbours)
    {
        Vector3 avgVel = new Vector3(0, 0, 0);
        int n = 0;

        foreach (Boid b in neighbours)
        {
            avgVel += b.getVel();
            n++;
        }

        if (n == 0) return new Vector3(0, 0, 0);
        return avgVel.normalized;
    }

    // Returns a direction vector for the cohesion part of the flocking behaviour
    private Vector3 cohesion(Boid[] neighbours)
    {
        Vector3 avgPos = new Vector3(0, 0, 0);
        int n = 0;

        foreach (Boid b in neighbours)
        {
            avgPos += b.getPos();
            n++;
        }

        if (n == 0) return new Vector3(0, 0, 0);
        return ((avgPos / n) - getPos()).normalized;
    }

    // Returns a direction vector for the separation part of the flocking behaviour
    private Vector3 separation(Boid[] neighbours)
    {
        Vector3 avgPos = new Vector3(0, 0, 0);
        int n = 0;

        foreach (Boid b in neighbours)
        {
            avgPos += b.getPos();
            n++;
        }

        if (n == 0) return new Vector3(0, 0, 0);
        return (getPos() - (avgPos / n)).normalized;
    }
}
