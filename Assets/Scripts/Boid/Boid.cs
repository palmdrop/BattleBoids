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
    [SerializeField] private float separationStrength = 1f;
    [SerializeField] private float avoidCollisionWeight = 5f;
    
    private BoidManager _manager;
    private Rigidbody _rigidbody;

    public LayerMask collisionMask;
    private Vector3 _localScale;
    private float _rayCastTheta = 10;

    float maxSpeed = 4;
    float minSpeed = 0.2f;
    float maxSteerForce = 1f;
    float impulseDamageThreshold = 0.2f;
    float targetHeight = 1f;
    float lastdY = 0;

    public Mesh mesh;
    private float collisionAvoidanceDistance; 

    public NeighborData computeShaderData;

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

        //Collision Avoidance, uses collisionMask

        if (HeadedForCollisionWithMapBoundary())
          force += SteerTowards( AvoidCollisionDir()) * avoidCollisionWeight * _rigidbody.velocity.magnitude;


        force += HoverForce();



        //Velocity change not using AddForce

        _rigidbody.velocity += force * Time.deltaTime;
        Vector3 velocity = _rigidbody.velocity;
        float speed = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
        Vector3 dir = new Vector3(velocity.x, 0, velocity.z) / (speed == 0 ? 1 : speed);
        speed = Mathf.Clamp(speed, 0, maxSpeed);
        _rigidbody.velocity = dir * speed + new Vector3(0,velocity.y,0);
        transform.forward = new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z);




        //Velocity change using AddForce

        /*
        _rigidbody.AddForce(force, ForceMode.Acceleration);
        transform.forward = _rigidbody.velocity;
        float tmp = Mathf.Clamp(_rigidbody.velocity.magnitude, minSpeed, maxSpeed);
        _rigidbody.velocity = ( _rigidbody.velocity.normalized* tmp);*/
    }


    //TODO: should not affect y-axis
    Vector3 SteerTowards(Vector3 vector)
    {
        Vector3 v = vector.normalized * maxSpeed - _rigidbody.velocity;
        float vel = _rigidbody.velocity.magnitude;

        //Function for improved velocity change at low speeds. Linear
        float m = 0;             //Min value at m
        float dy = (maxSpeed - m);          //Max value at maxSpeed
        float dx = (maxSpeed - minSpeed);

        return Vector3.ClampMagnitude(v, maxSteerForce * (vel > 0 ?  vel : 1)) ;   //Conditional operator if m = 0
    }

    private Vector3 HoverForce()
    {
        //Get map with heightmap
        GameObject map = GameObject.FindGameObjectWithTag("Map");
        Map.Map script = (Map.Map)map.GetComponent(typeof(Map.Map));

        //Calculate difference in height
        float targetYPos = targetHeight + script.HeightmapLookup(GetPos());
        float currentYPos = GetPos().y;

        //If boid exits map
        float deltaY = targetYPos > -1000? targetYPos - currentYPos : -100;

        //Formula to determine whether to hover or fall
        Vector3 yForce = new Vector3(0, (deltaY > 0 ? (5 * (deltaY - lastdY) / Time.fixedDeltaTime + 0.5f) : 10) * deltaY, 0);

        lastdY = deltaY;

        return yForce;
    }


    private bool HeadedForCollisionWithMapBoundary()
    {

        for (int i = 0; i < 3; i++) //Send 3 rays. This is to avoid tangentially going too close to an obstacle.
        {
            float angle = ((i + 1) / 2) * _rayCastTheta;    // series 0, theta, theta, 2*theta, 2*theta...
            int sign = i % 2 == 0 ? 1 : -1;                 // series 1, -1, 1, -1...

            Vector3 dir = RotationMatrix_y(angle * sign, _rigidbody.velocity).normalized;

            Ray ray = new Ray(GetPos() + GetCenterForwardPoint(), dir);
            

            if (Physics.Raycast(ray, collisionAvoidanceDistance, collisionMask ))   //Cast rays to nearby boundaries
            {
                //Debug.DrawLine(ray.origin, ray.origin + ray.direction * collisionAvoidanceDistance, Color.red);
                return true;
            }
            //Debug.DrawLine(ray.origin, ray.origin + ray.direction * collisionAvoidanceDistance, Color.green);

        }
        return false;
    }

    private Vector3 AvoidCollisionDir()
    {
        for (int i = 3; i < 300/_rayCastTheta; i++)
        {
            float angle = ((i + 1) / 2) * _rayCastTheta;    // series 0, theta, theta, 2*theta, 2*theta...
            int sign = i % 2 == 0 ? 1 : -1;                 // series 1, -1, 1, -1...

            Vector3 dir = RotationMatrix_y(angle*sign, _rigidbody.velocity).normalized;

            Ray ray = new Ray(GetPos() + GetCenterForwardPoint(), dir);

            if (!Physics.Raycast(ray, collisionAvoidanceDistance, collisionMask))   //Cast rays to nearby boundaries
            {
                // Debug.DrawLine(ray.origin, ray.origin + ray.direction * _collisionAvoidanceDistance, Color.green);   //Debug, uncomment to draw free path

                //Should only affect turn component of velocity. Should not accellerate forwards or backwards.
                return sign < 0 ? transform.right : -transform.right ;
            }

            //Debug.DrawLine(ray.origin, ray.origin + ray.direction * _collisionAvoidanceDistance, Color.red);  //Debug, uncomment to draw obstructed path
        }

        return transform.forward;
    }


    //Apply rotation along Y-axis
    private Vector3 RotationMatrix_y(float angle, Vector3 vector)
    {
        float cos = math.cos(angle * math.PI / 180);
        float sin = math.sin(angle * math.PI / 180);

        return new Vector3(vector.x * cos - vector.z * sin, 0, vector.x * sin + vector.z * cos);
    }


    //Returns the position of boid tip
    private Vector3 GetCenterForwardPoint()
    {
        return new Vector3(transform.forward.x * _localScale.x * mesh.bounds.size.z / 2, mesh.bounds.size.z * _localScale.y, transform.forward.z * _localScale.z * mesh.bounds.size.z / 2);
    }

    private Vector3 GetMiddlePoint()
    {
        return new Vector3(0, mesh.bounds.size.z * _localScale.y, 0);
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

    //Set collision detection distance
    public void setCollisionAvoidanceDistance(float val)
    {
        collisionAvoidanceDistance = val;
    }

    private NeighborData CalculateNeighbors(Boid[] neighbors)
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
        for (int i = 0; i < neighbors.Length; i++)
        {
            Boid b = neighbors[i];

            // Compare the distance between this boid and the neighbour using the
            // square of the distance and radius. This avoids costly square root operations
            Vector3 offset = (b.GetPos() - GetPos());
            Vector3 xzOffset = new Vector3(offset.x, 0, offset.z);
            float sqrDist = offset.sqrMagnitude;
            float sqrDistxz = xzOffset.sqrMagnitude;
            if (sqrDistxz < viewRadius * viewRadius)
            {
                // Add to average velocity
                avgVel += b.gameObject.transform.forward;
                viewCount++;

                // Add to average position for cohesion
                avgPosCohesion += offset;

            }
            // And if close enough, add to average position for separation
            if (sqrDist < separationRadius * separationRadius)
            {
                avgPosSeparation -= offset / sqrDist;
                separationViewCount++;
            }
        }

        return new NeighborData(avgVel, avgPosCohesion, avgPosSeparation, viewCount, separationViewCount);
    }

    public struct NeighborData
    {
        public Vector3 avgVel;
        public Vector3 avgPosCohesion;
        public Vector3 avgPosSeparation;
        public int viewCount;
        public int separationViewCount;

        public NeighborData(Vector3 avgVel, Vector3 avgPosCohesion, Vector3 avgPosSeparation, int viewCount, int separationViewCount)
        {
            this.avgVel = avgVel;
            this.avgPosCohesion = avgPosCohesion;
            this.avgPosSeparation = avgPosSeparation;
            this.viewCount = viewCount;
            this.separationViewCount = separationViewCount;
        }
    }

    private Vector3 CalculateSteeringForce(Boid[] neighbours)
    {
        NeighborData data = CalculateNeighbors(neighbours);

        // Calculate alignment force
        Vector3 alignmentForce;
        data.avgVel = new Vector3(data.avgVel.x, 0, data.avgVel.z);
        if (data.viewCount == 0) alignmentForce = new Vector3(0, 0, 0);
        else alignmentForce = SteerTowards(data.avgVel / data.viewCount ) * alignmentStrength;
        
        // Calculate cohesion force
        Vector3 cohesionForce;
        data.avgPosCohesion = new Vector3(data.avgPosCohesion.x, 0, data.avgPosCohesion.z);
        if (data.viewCount == 0) cohesionForce = new Vector3(0, 0, 0);
        else cohesionForce = SteerTowards((data.avgPosCohesion / data.viewCount) - GetPos()) * cohesionStrength;
        
        // Calculate separation force
        Vector3 separationForce;
        data.avgPosSeparation = new Vector3(data.avgPosSeparation.x, 0, data.avgPosSeparation.z);
        if (data.separationViewCount == 0) separationForce = new Vector3(0, 0, 0);
        else separationForce = SteerTowards(((data.avgPosSeparation ))) * separationStrength;

        return alignmentForce + cohesionForce + separationForce;
    }

    private void takeCollisionDamage(float impulse)
    {
        //Take damage according to some formula
    }

    //Function to run on collision as determined by unity boxcollider.
    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody b = collision.gameObject.GetComponent<Rigidbody>();
        if (b != null)
        {
            float collisionImpulse = collision.impulse.magnitude;
            if (collisionImpulse > impulseDamageThreshold)
            {
                takeCollisionDamage(collisionImpulse);
            }

        }
        else
        {
            float collisionImpulse = collision.impulse.magnitude;
            if (collisionImpulse > impulseDamageThreshold)
            {
                takeCollisionDamage(collisionImpulse);
            }
        }
    }
}
