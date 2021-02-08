using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Boid : MonoBehaviour
{
    [SerializeField] private float viewRadius = 5f;
    [SerializeField] private float separationRadius = 2f;
    [SerializeField] private float alignmentStrength = 0.1f;
    [SerializeField] private float cohesionStrength = 0.2f;
    [SerializeField] private float separationStrength = 3f;
    
    private BoidManager _manager;
    private Rigidbody _rigidbody;

    public LayerMask collisionMask;
    private Vector3 _localScale;
    private float _rayCastTheta = 10;

    public Mesh mesh;
    private float _sphereRadius = .1f;
    private float _collisionAvoidanceDistance = 1f;

    // Start is called before the first frame update
    void Start()
    {
        _manager = gameObject.GetComponentInParent<BoidManager>();
        _rigidbody = GetComponent<Rigidbody>();
        _localScale = transform.GetChild(0).transform.localScale;
    }

    // Called by the boid manager
    // Updates the boid according to the standard flocking behaviour
    public void UpdateBoid()
    {
        Boid[] neighbours = _manager.FindBoidsWithinRadius(this, Mathf.Max(viewRadius, separationRadius));
        Vector3 force = CalculateSteeringForce(neighbours);

        HeadedForCollisionWithMapBoundary();
        AvoidCollisionDir();

        _rigidbody.AddForce(force, ForceMode.Acceleration);
        transform.forward = _rigidbody.velocity;
    }

    private bool HeadedForCollisionWithMapBoundary()
    {


        //Ray ray = new Ray(GetPos() + getCenterForwardPoint(), transform.forward);
        //Debug.DrawLine(ray.origin, ray.origin + ray.direction* _collisionAvoidanceDistance);

        if (Physics.SphereCast(GetPos(), _sphereRadius, transform.forward, out _, _collisionAvoidanceDistance, collisionMask))
        {
            //Debug.Log("colliding");
            return true;
        }
        else
            //Debug.Log("not colliding");
            return false;
    }

    private Vector3 AvoidCollisionDir()
    {
        for (int i = 0; i < 270/_rayCastTheta; i++)
        {
            float angle = ((i + 1) / 2) * _rayCastTheta;    // series 0, theta, theta, 2*theta, 2*theta...
            int sign = i % 2 == 0 ? 1 : -1;                 // series 1, -1, 1, -1...

            float cos = math.cos(angle  *math.PI / 180 * sign);
            float sin = math.sin(angle * math.PI / 180 * sign);

            Vector3 dir = transform.forward;
            dir = new Vector3(dir.x*cos - dir.z*sin, 0, dir.x*sin + dir.z*cos); //Rotation matrix formula



            return dir;

        }

        return new Vector3(0,0,0);
    }

    private Vector3 getCenterForwardPoint()
    {
        return new Vector3(transform.forward.x * _localScale.x * mesh.bounds.size.z / 2, mesh.bounds.size.z * _localScale.y, transform.forward.z * _localScale.z * mesh.bounds.size.z / 2);
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
        for (int i = 0; i < neighbours.Length; i++)
        {
            Boid b = neighbours[i];

            // Compare the distance between this boid and the neighbour using the
            // square of the distance and radius. This avoids costly square root operations
            float sqrDist = (this.GetPos() - b.GetPos()).sqrMagnitude;
            if (sqrDist < viewRadius * viewRadius)
            {
                // Add to average velocity
                avgVel += b.GetVel();
                viewCount++;

                // Add to average position for cohesion
                avgPosCohesion += b.GetPos();

            }
            // And if close enough, add to average position for separation
            if (sqrDist < separationRadius * separationRadius)
            {
                avgPosSeparation += b.GetPos();
                separationViewCount++;
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
