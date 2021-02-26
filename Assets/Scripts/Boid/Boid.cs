using UnityEngine;
using Unity.Mathematics;


public abstract class Boid : Selectable
{
    protected int cost;
    protected int health;
    protected int maxHealth;
    protected int damage;
    protected float maxSpeed;
    protected float targetHeight;
    protected float collisionAvoidanceDistance;
    protected float avoidCollisionWeight;
    protected float hover_Ki;
    protected float hover_Kp;
    protected float timeBetweenAttacks;
    protected bool dead;
    protected Mesh mesh;
    protected LayerMask collisionMask;
    protected ClassInfo classInfo;
    protected Boid target;
    protected Player owner;

    public struct ClassInfo {
        // The field of view of the boid
        public float viewRadius;
        public float separationRadius;
        public float fearRadius;
        public float maxForce;

        // Weights for the three basic flocking behaviors
        // NOTE: an exponent of 0.0 would make the behavior ignore the distance to the neighbouring boid
        public float alignmentStrength, alignmentExponent;
        public float cohesionStrength, cohesionExponent;
        public float separationStrength, separationExponent;

        // Fear keeps boid from moving too close to enemies
        public float fearStrength, fearExponent; 
        
        // Attack range
        public float attackDstRange;
        public float attackAngleRange; // Angle relative local z-axis in rad
        
        public float attackMovementStrength, attackMovementExponent; // Controls attack impulse
        
        // Internal state of boid
        public float emotionalState;
        public float morale;
        public float aggressionStrength; // Controls how much the boid is attracted to the enemy flock

        // Misc behaviors
        public float randomMovements;
    }

    public struct BoidInfo {
        public float3 vel;
        public float3 pos;
        public float3 forward;
        public ClassInfo classInfo;
        public int flockId;

        public bool Equals(BoidInfo other)
        {
            return vel.Equals(other.vel) && pos.Equals(other.pos) && flockId == other.flockId;
        }
    }

    private Rigidbody _rigidbody;
    private Vector3 _localScale;
    private float _rayCastTheta = 10;
    private Map.Map _map;

    // Start is called before the first frame update
    protected void Start()
    {
        // To start off, we don't want to show that the boid is selected 
        SetSelectionIndicator(false);
        
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

    public void SetOwner(Player owner) {
        this.owner = owner;
    }

    public void SetTarget(Boid target) {
        this.target = target;
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
        info.forward = transform.forward;
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

    public float GetMaxHealth()
    {
        return maxHealth;
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
        target = null;
    }

    public bool IsDead() {
        return dead;
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

    public Rigidbody GetRigidbody()
    {
        return _rigidbody;
    }

    public abstract void Attack();
    
    
}
