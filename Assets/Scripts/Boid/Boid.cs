using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Boid : MonoBehaviour
{
    [SerializeField] private int cost = 10;
    [SerializeField] private int health = 100;
    [SerializeField] private int damage = 10;
    [SerializeField] private float maxSpeed = 5f;

    public struct ClassInfo {
        public float separationRadius;
        public float viewRadius;
        public float alignmentStrength;
        public float cohesionStrength;
        public float separationStrength;
        public float emotionalState;
        public float morale;
        public float aggressionRadius;
        public float aggressionStrength;
    }

    public struct BoidInfo {
        public float3 vel;
        public float3 pos;
        public ClassInfo classInfo;
        public int flockId;
    }

    public bool dead = false;

    private ClassInfo classInfo = new ClassInfo
    {
        separationRadius = 0.2f,
        viewRadius = 5f,
        alignmentStrength = 1.1f,
        cohesionStrength = 1.2f,
        separationStrength = 3f,
        emotionalState = 0f,
        morale = 0f,
        aggressionStrength = 1f
    };

    private Rigidbody _rigidbody;
    private Player owner;

    // Start is called before the first frame update
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
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

    private void OnCollisionEnter(Collision collision) {
        TakeDamage((int) collision.impulse.magnitude * 10);
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

}
