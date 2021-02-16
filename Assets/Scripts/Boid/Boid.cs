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
    private Vector3 _localScale;
    private Player owner;
    public Mesh mesh;

    // Start is called before the first frame update
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _localScale = transform.GetChild(0).transform.localScale;
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

    private Vector3 GetCenterForwardPoint()
    {
        return new Vector3(transform.forward.x * _localScale.x * mesh.bounds.size.z / 2, mesh.bounds.size.z * _localScale.y, transform.forward.z * _localScale.z * mesh.bounds.size.z / 2);
    }

    private Vector3 GetMiddlePoint()
    {
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

}
