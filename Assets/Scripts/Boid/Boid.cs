using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Boid : MonoBehaviour
{
    [SerializeField] private int cost = 10;
    [SerializeField] private int health = 100;
    [SerializeField] private int damage = 10;
    [SerializeField] private float maxSpeed = 2f;
    [SerializeField] private float targetHeight = 1f;
    [SerializeField] private float collisionAvoidanceDistance;
    [SerializeField] private float avoidCollisionWeight = 5f;
    [SerializeField] private float hover_Ki = 5f;
    [SerializeField] private float hover_Kp = 0.5f;
    [SerializeField] private float hover_gravity = 10f;

    public struct ClassInfo {
        //public float separationRadius;
        public float viewRadius;
        
        public float alignmentStrength, alignmentExponent;
        public float cohesionStrength, cohesionExponent;
        public float separationStrength, separationExponent;
        
        public float emotionalState;
        public float morale;
        
        public float aggressionRadius;
        public float aggressionStrength;

        public float fearStrength, fearExponent;
        public float randomMovements;
    }

    public struct BoidInfo {
        public float3 vel;
        public float3 pos;
        public ClassInfo classInfo;
        public int flockId;
    }

    public bool dead = false;
    public Mesh mesh;
    public LayerMask collisionMask;

    private ClassInfo classInfo = new ClassInfo
    {
        //separationRadius = 0.3f,
        viewRadius = 5f,
        
        alignmentStrength = 0.5f, alignmentExponent = 1.0f,
        cohesionStrength = 0.9f, cohesionExponent = 1.0f,
        separationStrength = 0.5f, separationExponent = 10.0f,
        
        emotionalState = 0f,
        morale = 1f,
        aggressionStrength = 1.5f,
        
        fearStrength = 0.8f, fearExponent = -0.6f,
        
        randomMovements = 2.0f,
    };

    private Rigidbody _rigidbody;
    private Vector3 _localScale;
    private Player owner;
    private float lastdY = 0;
    private float _rayCastTheta = 10;
    private Map.Map map;

    // Start is called before the first frame update
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _localScale = transform.GetChild(0).transform.localScale;
        GameObject map = GameObject.FindGameObjectWithTag("Map");
        if (map != null)
        {
            this.map = (Map.Map)map.GetComponent(typeof(Map.Map));
        }
    }

    // Called by the boid manager
    // Updates the boid according to the standard flocking behaviour
    public void UpdateBoid(Vector3 force)
    {

        _rigidbody.AddForce(force, ForceMode.Acceleration);

        if (_rigidbody.velocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            _rigidbody.velocity = _rigidbody.velocity.normalized * maxSpeed;
        }
        transform.forward = _rigidbody.velocity;

    }

    private Vector3 HoverForce()
    {
        if (map == null)
        {
            return Vector3.zero;
        }
        //Calculate difference in height
        float targetYPos = targetHeight + map.HeightmapLookup(GetPos());
        float currentYPos = GetPos().y;

        //If boid exits map
        float deltaY = targetYPos > -1000 ? targetYPos - currentYPos : -100;

        //Formula to determine whether to hover or fall, uses a PI-regulator with values Ki and Kp
        Vector3 yForce = new Vector3(0, (deltaY > 0 ? (hover_Ki * (deltaY - lastdY) / Time.fixedDeltaTime + hover_Kp * deltaY) : hover_gravity) * deltaY, 0);

        lastdY = deltaY;

        return yForce;
    }

    private bool HeadedForCollisionWithMapBoundary()
    {

        if (collisionMask == null)
        {
            return false;
        }

        for (int i = 0; i < 3; i++) //Send 3 rays. This is to avoid tangentially going too close to an obstacle.
        {
            float angle = ((i + 1) / 2) * _rayCastTheta;    // series 0, theta, theta, 2*theta, 2*theta...
            int sign = i % 2 == 0 ? 1 : -1;                 // series 1, -1, 1, -1...

            Vector3 dir = RotationMatrix_y(angle * sign, GetVel()).normalized;

            Ray ray = new Ray(GetPos() + GetCenterForwardPoint(), dir);


            if (Physics.Raycast(ray, collisionAvoidanceDistance, collisionMask))   //Cast rays to nearby boundaries
            {
                return true;
            }

        }
        return false;
    }

    private Vector3 AvoidCollisionDir()
    {
        if (collisionMask == null)
        {
            return transform.forward;
        }

        for (int i = 3; i < 300 / _rayCastTheta; i++)
        {
            float angle = ((i + 1) / 2) * _rayCastTheta;    // series 0, theta, theta, 2*theta, 2*theta...
            int sign = i % 2 == 0 ? 1 : -1;                 // series 1, -1, 1, -1...

            Vector3 dir = RotationMatrix_y(angle * sign, GetVel()).normalized;

            Ray ray = new Ray(GetPos() + GetCenterForwardPoint(), dir);

            if (!Physics.Raycast(ray, collisionAvoidanceDistance, collisionMask))   //Cast rays to nearby boundaries
            {
                //Should only affect turn component of velocity. Should not accellerate forwards or backwards.
                return sign < 0 ? transform.right : -transform.right;
            }
        }
        return transform.forward;
    }

    private void OnCollisionEnter(Collision collision) {
        //TakeDamage((int) collision.impulse.magnitude * 10);
    }

    public void SetOwner(Player owner) {
        this.owner = owner;



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

    public BoidInfo GetInfo() {
        BoidInfo info;
        info.pos = GetPos();
        info.vel = GetVel();
        info.classInfo = classInfo;
        info.flockId = owner.id;
        return info;
    }

    public int GetCost()
    {
        return cost;
    }

    public float GetHealth()
    {
        return health;
    }

    public int GetDamage()
    {
        return damage;
    }

    public void TakeDamage(int damageTaken)
    {
        health = math.max(health - damageTaken, 0);
        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        this.dead = true;
    }

    private Vector3 GetCenterForwardPoint()
    {
        if (mesh == null || _localScale == null)
            return Vector3.zero;
        return new Vector3(transform.forward.x * _localScale.x * mesh.bounds.size.z / 2, mesh.bounds.size.z * _localScale.y, transform.forward.z * _localScale.z * mesh.bounds.size.z / 2);
    }

    private Vector3 GetMiddlePoint()
    {
        if (mesh == null || _localScale == null)
            return Vector3.zero;
        return new Vector3(0, mesh.bounds.size.z * _localScale.y, 0);
    }

    private Vector3 RotationMatrix_y(float angle, Vector3 vector)
    {
        float cos = math.cos(angle * math.PI / 180);
        float sin = math.sin(angle * math.PI / 180);

        return new Vector3(vector.x * cos - vector.z * sin, 0, vector.x * sin + vector.z * cos);
    }

    private Vector3 RemoveYComp(Vector3 v)
    {
        return new Vector3(v.x, 0, v.z);
    }

    private Vector3 GetYComp(Vector3 v)
    {
        return new Vector3(0, v.y, 0);
    }

    public void SetColor(Color color)
    {
        //NOTE: ugly solution, assumes prefab structure... TODO improve somehow
        transform.GetChild(0).transform.GetChild(0).GetComponent<MeshRenderer>().material.SetColor("_Color", color);
    }
}
