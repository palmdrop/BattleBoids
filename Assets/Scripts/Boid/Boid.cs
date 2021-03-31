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
    
    public static string GetDescription(string name)
    {
        string description;
        switch (name)
        {
            case "Melee":
                description = "Fast and cheap and does close range damage";
                break;
            case "Ranged":
                description = "Keeps a distance and shoots projectiles";
                break;
            case "Hero":
                description = "Strong and charges up an explosive shot";
                break;
            case "Scarecrow":
                description = "Sly and scary, repels enemies";
                break;
            case "Healer":
                description = "Careful and peaceful, but heals allies";
                break;
            case "Commander":
                description = "Leader type, follows a path and tries to make allies to join";
                break;
            default:
                return "[NO SUCH TYPE]";
        }

        return description;
    }

    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private GameObject deathAnimationPrefab;
    [SerializeField] protected LayerMask collisionMask;
    [SerializeField] protected LayerMask groundMask;
    [SerializeField] protected Material baseMaterial;

    public static List<Material> materials = new List<Material>();

    private Rigidbody _rigidbody;
    private Collider _collider;
    private Material _material;
    private Vector3 _localScale;
    private bool _hasMaterial = false;
    // Cache shader property to avoid expensive shader uniform lookups
    //private static readonly int Color = Shader.PropertyToID("_Color");
    [SerializeField] private AudioClip collisionAudio;
    
    private Map.Map _map;
    private bool _hasMap = false;
    
    private GameObject _healthBar;
    private float _rayCastTheta = 10;
    private float _previousActionTime = 0.0f;
    
    private bool _dead;

    protected Type type;
    protected Player owner;
    
    protected Boid target;
    private bool _hasTarget = false;
    protected Boid friendlyTarget;
    private bool _hasFriendlyTarget = false;
    protected float boostUntil;
    
    protected int health;
    protected int maxHealth;
    protected int damage;
    protected int boostedDamage;
    protected float maxSpeed;
    protected float collisionAvoidanceDistance;
    protected float avoidCollisionWeight;
    protected float timeBetweenActions;
    
    protected float emotionalState;
    protected float morale;
    protected float moraleDefault;
    protected float abilityDistance;

    public float3 hoverForce;

    public struct ClassInfo {
        public Type type;
        // The field of view of the boid
        public float viewRadius;
        public float separationRadius;
        public float fearRadius;
        public float maxForce;
        
        // The confidence threshold controls how friendly boids / enemy boids are required
        // for the boid to remain confident. When a boid looses confidence, they will no longer be
        // aggressive and will start searching for friendly boids instead.
        public float confidenceThreshold;

        // Weights for the three basic flocking behaviors
        // NOTE: an exponent of 0.0 would make the behavior ignore the distance to the neighbouring boid
        public float alignmentStrength, alignmentExponent;
        public float cohesionStrength, cohesionExponent;
        public float separationStrength, separationExponent;
        public float avoidCollisionWeight;

        public float collisionAvoidanceDistance;
        public uint collisionMask;
        public uint groundMask;
        public float abilityDistance;
        public float maxHealth;

        // How much this unit affects friendly units
        public float gravity;

        // Fear keeps boid from moving too close to enemies
        public float fearStrength, fearExponent; 
        
        // Attack range
        public float attackDistRange;
        public float attackAngleRange; // Angle relative local z-axis in rad
        
        public float approachMovementStrength, approachMovementExponent; // Controls attack impulse


        public float aggressionStrength; // Controls how much the boid is attracted to the enemy flock
        public float aggressionFalloff; // A high value will reduce the aggression drastically when a boid moves
                                        // closer to the enemy flock
        public float aggressionDistanceCap;  // If the enemy flock is further away than this, the aggression will be at max strength
        public float maxAggressionMultiplier;  // Maximum aggression multiplier. A flock with an advantage will be more aggressive

        public float searchStrength; // Controls how much the boid is attracted to the center of the allied flock
                                     // This behavior is only active if the boid has a low confidence level

        public float avoidanceStrength; // A boid tries to avoid being in the attack scope of an enemy boid

        // Misc behaviors
        public float randomMovements;

        public float targetHeight;
        public float hoverKi;
        public float hoverKp;

        // Only used by scarecrow
        public float fearMultiplier;
    }

    // Struct for holding relevant information about the boid
    // This struct is used in a Burst job in order to calculate the forces 
    // acting on the boid, among other things.
    public struct BoidInfo {
        public Type type;
        public float3 vel;
        public float3 pos;
        public float3 forward;
        public float3 right;
        public int health;
        
        public int flockId;
        public float emotionalState;
        public float morale;
        public float moraleDefault;

        public bool isBoosted;

        public bool Equals(BoidInfo other)
        {
            return vel.Equals(other.vel) && pos.Equals(other.pos) && flockId == other.flockId;
        }
    }

    // Start is called before the first frame update
    protected void Start()
    {
        owner = GetComponentInParent<Player>();
        SetColor(owner.color);

        // To start off, we don't want to show that the boid is selected 
        SetSelectionIndicator(false);
        
        collisionMask = LayerMask.GetMask("Wall", "Obstacle");
        groundMask = LayerMask.GetMask("Ground", "Obstacle");

        _dead = false;
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _collider.enabled = false;
        
        GameObject map = GameObject.FindGameObjectWithTag("Map");
        if (map != null)
        {
            _hasMap = true;
            this._map = (Map.Map)map.GetComponent(typeof(Map.Map));
        }
        _localScale = transform.GetChild(0).transform.localScale;
        //_healthBar = Instantiate(healthBarPrefab, transform);
    }

    public void StartBoid()
    {
        _collider.enabled = true;
        _rigidbody.useGravity = true;
    }

    public void FixedUpdate()
    {
        if(_dead)
            return;
        _rigidbody.AddForce(hoverForce, ForceMode.Acceleration);

        // Wait until next action is ready
        if ((Time.time - _previousActionTime) >= timeBetweenActions)
        {
            Act();
            _previousActionTime = Time.time;
        }

    }

    // Called by the boid manager
    // Updates the boid according to the standard flocking behaviour
    public virtual void UpdateBoid(Vector3 force)
    {
        hoverForce = new float3(0,force.y,0);
        _rigidbody.AddForce(RemoveYComp(force), ForceMode.Acceleration);

        if (_rigidbody.velocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            _rigidbody.velocity = _rigidbody.velocity.normalized * maxSpeed;
        }

        Vector3 velocity = _rigidbody.velocity;

        // Only update forward direction if velocity is non-zero
        if (velocity != Vector3.zero)
        {
            transform.forward = new Vector3(velocity.x, 0, velocity.z);
        }
    }

    private void OnCollisionEnter(Collision collision) {
        //TakeDamage((int) collision.impulse.magnitude * 10);

        // Plays a collision sound
        // Currently only plays the sound when colliding when an object in the Obstacle layer
        // The reason for not playing the sound when colliding with a wall is that it was very
        // unclear why the sound was played, since the walls are invisible
        // If the sound should be played when colliding with a wall,
        // uncomment the part below
        if (/*collision.collider.gameObject.layer == LayerMask.NameToLayer("Wall")
            || */collision.collider.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            AudioManager.instance.PlaySoundEffectAtPoint(collisionAudio, GetPos(), 0.5f);
        }
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

    public void SetFriendlyTarget(Boid target) {
        this.friendlyTarget = target;
        _hasFriendlyTarget = friendlyTarget != null;
    }

    public bool HasTarget()
    {
        return _hasTarget;
    }

    public bool HasFriendlyTarget()
    {
        return _hasFriendlyTarget;
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
        info.flockId = owner.id;
        info.emotionalState = emotionalState;
        info.morale = morale;
        info.moraleDefault = moraleDefault;
        info.right = transform.right;
        info.isBoosted = IsBoosted();
        return info;
    }

    public int GetCost()
    {
        return Cost;
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

    public Type GetType()
    {
        return type;
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

    public void GiveBoost(float time)
    {
        boostUntil =  Time.time + time;
    }

    public bool IsBoosted()
    {
        return boostUntil > Time.time;
    }

    public void Die()
    {
        if (!_dead) {
            _dead = true;
            SetTarget(null);
            Destroy(gameObject);
            AnimateDeath();
        }
    }

    private void AnimateDeath() {
        GameObject death = Instantiate(deathAnimationPrefab, transform.position, transform.rotation);
        ParticleSystem.MainModule psMain = death.GetComponent<ParticleSystem>().main;
        psMain.startColor = owner.color;
        death.GetComponent<Rigidbody>().velocity = gameObject.GetComponent<Rigidbody>().velocity;
        Destroy(death, psMain.duration);
    }

    public bool IsDead() {
        return _dead;
    }

    protected Vector3 RemoveYComp(Vector3 v)
    {
        return new Vector3(v.x, 0, v.z);
    }

    public void SetColor(Color color)
    {
        foreach (Material material in materials)
        {
            if (color.Equals(material.color))
            {
                transform.GetChild(0).transform.GetChild(0).GetComponent<MeshRenderer>().material = material;
                return;
            }
        }
        Material tmp = new Material(baseMaterial);
        tmp.color = color;
        materials.Add(tmp);
        transform.GetChild(0).transform.GetChild(0).GetComponent<MeshRenderer>().material = tmp;
    }


    public Rigidbody GetRigidbody()
    {
        return _rigidbody;
    }

    public virtual void SetHidden(bool hidden)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) {
            r.enabled = !hidden;
        }
    }

    protected abstract void Act();
}
