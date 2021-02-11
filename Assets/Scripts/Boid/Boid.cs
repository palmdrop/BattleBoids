using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Boid : MonoBehaviour
{
    public struct ClassInfo {
        public float separationRadius;
        public float viewRadius;
        public float alignmentStrength;
        public float cohesionStrength;
        public float separationStrength;
    }

    public struct BoidInfo {
        public float3 vel;
        public float3 pos;
        public ClassInfo classInfo;
    }

    private ClassInfo classInfo = new ClassInfo
    {
        separationRadius = 1f,
        viewRadius = 5f,
        alignmentStrength = 1.1f,
        cohesionStrength = 1.2f,
        separationStrength = 3f
    };

    private Rigidbody _rigidbody;

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
        transform.forward = _rigidbody.velocity;
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
        return info;
    }

}
