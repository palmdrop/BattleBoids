using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

public abstract class Boid : Selectable
{
    // The possible boid types
    public enum Type {
        Melee,
        Ranged,
        Hero,
        Scarecrow,
        Healer,
        Commander
    }
    
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private LayerMask collisionMask;
    
    private Rigidbody _rigidbody;
    private Material _material;
    private bool _hasMaterial = false;
    // Cache shader property to avoid expensive shader uniform lookups
    private static readonly int Color = Shader.PropertyToID("_Color");
    
    private Map.Map _map;
    private bool _hasMap = false;
    
    private GameObject _healthBar;
    private float _rayCastTheta = 10;
    private float _previousActionTime = 0.0f;
    
    private bool _dead;

    protected Type type;
    protected ClassInfo classInfo;
    protected Player owner;
    
    protected Boid target;
    private bool _hasTarget = false;
    
    protected int cost;
    protected int health;
    protected int maxHealth;
    protected int damage;
    protected float maxSpeed;
    protected float targetHeight;
    protected float collisionAvoidanceDistance;
    protected float avoidCollisionWeight;
    protected float hoverKi;
    protected float hoverKp;
    protected float timeBetweenActions;
    
    protected float emotionalState;
    protected float morale;
    protected float moraleDefault;
    protected float abilityDistance;

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
        public float attackDistRange;
        public float attackAngleRange; // Angle relative local z-axis in rad
        
        public float approachMovementStrength, approachMovementExponent; // Controls attack impulse
        
        public float aggressionStrength; // Controls how much the boid is attracted to the enemy flock

        // Misc behaviors
        public float randomMovements;
    }

    // Struct for holding relevant information about the boid
    // This struct is used in a Burst job in order to calculate the forces 
    // acting on the boid, among other things.
    public struct BoidInfo {
        public Type type;
        public float3 vel;
        public float3 pos;
        public float3 forward;
        public int health, maxHealth;
        
        public ClassInfo classInfo;
        public int flockId;
        public float emotionalState;
        public float morale;
        public float moraleDefault;
        public float abilityDistance;

        public bool Equals(BoidInfo other)
        {
            return vel.Equals(other.vel) && pos.Equals(other.pos) && flockId == other.flockId;
        }
    }

    // Start is called before the first frame update
    protected void Start()
    {
        // To start off, we don't want to show that the boid is selected 
        SetSelectionIndicator(false);
        
        //_collisionMask = LayerMask.GetMask("Wall", "Obstacle");
        _dead = false;
        _rigidbody = GetComponent<Rigidbody>();
        
        GameObject map = GameObject.FindGameObjectWithTag("Map");
        if (map != null)
        {
            _hasMap = true;
            this._map = (Map.Map)map.GetComponent(typeof(Map.Map));
        }

        _healthBar = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
        _healthBar.GetComponent<HealthBar>().SetOwner(this);
        
    }

    public void FixedUpdate()
    {
        _rigidbody.AddForce(HoverForce(), ForceMode.Acceleration);

        if (!_dead && HeadedForCollisionWithMapBoundary()) {
            _rigidbody.AddForce(AvoidCollisionDir() * avoidCollisionWeight, ForceMode.Acceleration);
        }

        // Wait until next action is ready
        if ((Time.time - _previousActionTime) >= timeBetweenActions)
        {
            Act();
            _previousActionTime = Time.time;
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

        Vector3 velocity = _rigidbody.velocity;
        transform.forward = new Vector3(velocity.x, 0, velocity.z);
    }

    private Vector3 HoverForce()
    {
        if (!_hasMap)
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
        Vector3 yForce = new Vector3(0, (deltaY > 0 && !_dead ? (hoverKp * deltaY - hoverKi * velY) : 0), 0);
        
        return yForce;
    }

    private bool HeadedForCollisionWithMapBoundary()
    {
        for (int i = 0; i < 3; i++) //Send 3 rays. This is to avoid tangentially going too close to an obstacle.
        {
            float angle = ((i + 1) / 2.0f) * _rayCastTheta;    // series 0, theta, theta, 2*theta, 2*theta...
            int sign = i % 2 == 0 ? 1 : -1;                 // series 1, -1, 1, -1...

            Vector3 dir = RotationMatrix_y(angle * sign, GetVel()).normalized;

            Ray ray = new Ray(GetPos(), dir);

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
            float angle = ((i + 1) / 2.0f) * _rayCastTheta;    // series 0, theta, theta, 2*theta, 2*theta...
            int sign = i % 2 == 0 ? 1 : -1;                 // series 1, -1, 1, -1...

            Vector3 dir = RotationMatrix_y(angle * sign, GetVel()).normalized;

            Ray ray = new Ray(GetPos(), dir);

            if (!Physics.Raycast(ray, collisionAvoidanceDistance, collisionMask))   //Cast rays to nearby boundaries
            {
                //Should only affect turn component of velocity. Should not accellerate forwards or backwards.
                var right = transform.right;
                return sign < 0 ? right : -right;
            }
        }
        return new Vector3(0, 0, 0);
    }

    private void OnCollisionEnter(Collision collision) {
        //TakeDamage((int) collision.impulse.magnitude * 10);
    }

    public List<Boid> FindEnemiesInSphere(Vector3 position, float radius, int layerMask) {
        List<Boid> boids = new List<Boid>();
        Collider[] colliders = Physics.OverlapSphere(position, radius, layerMask);
        foreach (Collider hit in colliders) {
            Boid boid = hit.GetComponent<Boid>();
            if (boid != null && boid.GetOwner() != owner) {
                boids.Add(boid);
            }
        }
        return boids;
    }

    public void SetOwner(Player owner) {
        this.owner = owner;
    }

    public Player GetOwner() {
        return owner;
    }

    public void SetTarget(Boid target) {
        this.target = target;
        _hasTarget = target != null;
    }

    public bool HasTarget()
    {
        return _hasTarget;
    }

    public void SetMorale(float morale) {
        this.morale = morale;
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
        info.type = type;
        info.pos = GetPos();
        info.forward = transform.forward;
        info.vel = GetVel();
        info.health = health;
        info.maxHealth = maxHealth;
        info.classInfo = classInfo;
        info.flockId = owner.id;
        info.emotionalState = emotionalState;
        info.morale = morale;
        info.moraleDefault = moraleDefault;
        info.abilityDistance = abilityDistance;
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

    public void ReceiveHealth(int healthReceived)
    {
        health = math.min(health + healthReceived, maxHealth);
    }

    public void Die()
    {
        this._dead = true;
        SetTarget(null);
        Destroy(GetComponent<ParticleSystem>());
    }

    public bool IsDead() {
        return _dead;
    }

    private Vector3 RotationMatrix_y(float angle, Vector3 vector)
    {
        float cos = math.cos(angle * math.PI / 180);
        float sin = math.sin(angle * math.PI / 180);

        return new Vector3(vector.x * cos - vector.z * sin, 0, vector.x * sin + vector.z * cos);
    }

    protected Vector3 RemoveYComp(Vector3 v)
    {
        return new Vector3(v.x, 0, v.z);
    }

    private Vector3 GetYComp(Vector3 v)
    {
        return new Vector3(0, v.y, 0);
    }

    public void SetColor(Color color)
    {
        // Cache material in order to avoid having to do multiple expensive lookups
        if(!_hasMaterial) {
            _material = transform.GetChild(0).transform.GetChild(0).GetComponent<MeshRenderer>().material;
            _hasMaterial = true;
        }
        _material.SetColor(Color, color);
    }

    public Rigidbody GetRigidbody()
    {
        return _rigidbody;
    }

    protected abstract void Act();
}
