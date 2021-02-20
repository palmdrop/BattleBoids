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
    [SerializeField] private float collisionAvoidanceDistance = 3f;
    [SerializeField] private float avoidCollisionWeight = 5f;
    [SerializeField] private float hover_Ki = 5f;
    [SerializeField] private float hover_Kp = 1f;
    [SerializeField] private float timeBetweenAttacks = 0.1f;

    public struct ClassInfo {
        public float viewRadius;

        public float attackDstRange;
        public float attackAngleRange;
        
        public float alignmentStrength, alignmentExponent;
        public float cohesionStrength, cohesionExponent;
        public float separationStrength, separationExponent;

        public float fearStrength, fearExponent;
        public float attackMovementStrength, attackMovementExponent;
        
        public float emotionalState;
        public float morale;
        public float aggressionStrength;

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
        // The field of view of the boid. 
        viewRadius = 5f,

        // Attack range
        attackDstRange = 0.5f,
        attackAngleRange = 45, // Angle relative local z-axis
        
        // Weights for the three basic flocking behaviors 
        // NOTE: an exponent of 0.0 would make the behavior ignore the distance to the neighbouring boid
        alignmentStrength = 0.7f, alignmentExponent = 1.0f, 
        cohesionStrength = 1.5f, cohesionExponent = 0.8f,
        separationStrength = 0.5f, separationExponent = 10.0f,
        
        // Additional behaviors
        fearStrength = 4.65f, fearExponent = 8.0f, // Fear controls 
        attackMovementStrength = 1.1f, attackMovementExponent = 5.0f, // Controls attack impulse
        
        // Internal state of boid
        emotionalState = 0f,
        morale = 1f,
        aggressionStrength = 2.0f, // Controls how much the boid is attracted to the enemy flock

        // Misc behaviors
        randomMovements = 6.0f,
    };

    private Boid _target;
    private float _nextAttackTime;
    private Rigidbody _rigidbody;
    private Vector3 _localScale;
    private Player _owner;
    private float _rayCastTheta = 10;
    private Map.Map _map;

    // Start is called before the first frame update
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        GameObject map = GameObject.FindGameObjectWithTag("Map");
        if (map != null)
        {
            this._map = (Map.Map)map.GetComponent(typeof(Map.Map));
        }
        _localScale = transform.GetChild(0).transform.localScale;
    }

    void Update() {
        Attack();
    }

    public void FixedUpdate()
    {
        _rigidbody.AddForce(HoverForce(), ForceMode.Acceleration);

        if (!dead && HeadedForCollisionWithMapBoundary()) {
            _rigidbody.AddForce(AvoidCollisionDir() * avoidCollisionWeight, ForceMode.Acceleration);
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
        transform.forward = new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z);
    }

    private Vector3 HoverForce()
    {
        if (_map == null)
        {
            return Vector3.zero;
        }
        //Calculate difference in height
        float targetYPos = targetHeight + _map.HeightmapLookup(GetPos());
        float currentYPos = GetPos().y;

        //If boid exits map
        float deltaY = targetYPos > -1000 ? targetYPos - currentYPos : -100;
        float velY = GetVel().y;

        //Formula to determine whether to hover or fall, uses a PI-regulator with values Ki and Kp
        Vector3 yForce = new Vector3(0, (deltaY > 0 && !dead ? (hover_Kp * deltaY - hover_Ki * velY) : 0), 0);
        
        return yForce;
    }

    private bool HeadedForCollisionWithMapBoundary()
    {
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
        return new Vector3(0, 0, 0);
    }

    private void OnCollisionEnter(Collision collision) {
        //TakeDamage((int) collision.impulse.magnitude * 10);
    }

    private void Attack() {
        if (_target != null && Time.time > _nextAttackTime) {
            _nextAttackTime = Time.time + timeBetweenAttacks;
            _target.TakeDamage(damage);
            AnimateAttack(this.GetPos(), _target.GetPos());
        }
    }

    private void AnimateAttack(Vector3 fromPos, Vector3 toPos) {
        LineRenderer lineRenderer = new GameObject("Line").AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPosition(0, fromPos);
        lineRenderer.SetPosition(1, toPos);
        Destroy(lineRenderer, 0.2f);
    }

    public void SetOwner(Player owner) {
        this._owner = owner;
    }

    public void SetTarget(Boid target) {
        _target = target;
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
        info.flockId = _owner.id;
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
        _target = null;
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
