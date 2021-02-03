using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{

    public BoidManager manager;
    public float viewRadius = 5f;
    public float separationRadius = 2f;
    public float alignmentStrength = 0.1f;
    public float cohesionStrength = 0.2f;
    public float separationStrength = 3f;

    // Start is called before the first frame update
    void Start()
    {
        manager = GameObject.FindGameObjectWithTag("BoidManager").GetComponent<BoidManager>();
    }

    // Update is called once per frame
    void Update()
    {

    }


    public void UpdateBoid()
    {
        Vector3 alignmentForce = alignment() * alignmentStrength;
        Vector3 cohesionForce = cohesion() * cohesionStrength;
        Vector3 separationForce = separation() * separationStrength;

        Rigidbody rb = GetComponent<Rigidbody>();

        Vector3 force = alignmentForce + cohesionForce + separationForce;


        rb.AddForce(force, ForceMode.Acceleration);

        transform.forward = rb.velocity;
    }


    public Vector3 getPos()
    {
        return GetComponent<Rigidbody>().position;
    }


    public Vector3 getVel()
    {
        return GetComponent<Rigidbody>().velocity;
    }


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
