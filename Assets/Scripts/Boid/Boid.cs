using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{

    private BoidManager manager;
    [SerializeField] private float viewRadius = 5f;
    [SerializeField] private float separationRadius = 2f;
    [SerializeField] private float alignmentStrength = 0.1f;
    [SerializeField] private float cohesionStrength = 0.2f;
    [SerializeField] private float separationStrength = 3f;

    // Start is called before the first frame update
    void Start()
    {
        manager = GameObject.FindGameObjectWithTag("BoidManager").GetComponent<BoidManager>();
    }

    // Called by the boid manager
    // Updates the boid according to the standard flocking behaviour
    public void UpdateBoid()
    {
        Vector3 alignmentForce = alignment() * alignmentStrength;
        Vector3 cohesionForce = cohesion() * cohesionStrength;
        Vector3 separationForce = separation() * separationStrength;

        Vector3 force = alignmentForce + cohesionForce + separationForce;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.AddForce(force, ForceMode.Acceleration);

        transform.forward = rb.velocity;
    }

    // Returns the position of this boid
    public Vector3 getPos()
    {
        return GetComponent<Rigidbody>().position;
    }

    // Returns the velocity of this boid
    public Vector3 getVel()
    {
        return GetComponent<Rigidbody>().velocity;
    }

    // Returns a direction vector for the alignment part of the flocking behaviour
    private Vector3 alignment()
    {
        Vector3 avgVel = new Vector3(0, 0, 0);
        int n = 0;

        Boid[] neighbours = manager.findBoidsWithinRadius(this, viewRadius);

        foreach (Boid b in neighbours)
        {
            avgVel += b.getVel();
            n++;
        }

        if (n == 0) return new Vector3(0, 0, 0);
        return avgVel.normalized;
    }

    // Returns a direction vector for the cohesion part of the flocking behaviour
    private Vector3 cohesion()
    {
        Vector3 avgPos = new Vector3(0, 0, 0);
        int n = 0;

        Boid[] neighbours = manager.findBoidsWithinRadius(this, viewRadius);

        foreach (Boid b in neighbours)
        {
            avgPos += b.getPos();
            n++;
        }

        if (n == 0) return new Vector3(0, 0, 0);
        return ((avgPos / n) - getPos()).normalized;
    }

    // Returns a direction vector for the separation part of the flocking behaviour
    private Vector3 separation()
    {
        Vector3 avgPos = new Vector3(0, 0, 0);
        int n = 0;

        Boid[] neighbours = manager.findBoidsWithinRadius(this, separationRadius);

        foreach (Boid b in neighbours)
        {
            avgPos += b.getPos();
            n++;
        }

        if (n == 0) return new Vector3(0, 0, 0);
        return (getPos() - (avgPos / n)).normalized;
    }
}
