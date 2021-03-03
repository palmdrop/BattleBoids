using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Ranged : Boid {

    [SerializeField] private GameObject projectilePrefeb;

    private float _projSpeed = 8;

    private Vector3 _p;
    private Vector3 _v;
    private Vector3 _g = Physics.gravity;

    // Start is called before the first frame update
    void Start() {
        base.Start();

        cost = 10;
        health = 100;
        damage = 25;
        maxSpeed = 4f;
        targetHeight = 2f;
        collisionAvoidanceDistance = 3f;
        avoidCollisionWeight = 5f;
        hoverKi = 2f;
        hoverKp = 10f;
        timeBetweenActions = 2f;
        dead = false;
        //mesh = ;
        collisionMask = LayerMask.GetMask("Wall", "Obstacle");


        classInfo = new ClassInfo {
            viewRadius = 3f,
            separationRadius = 0.3f,
            fearRadius = 1.0f,
            maxForce = 2f,
            
            alignmentStrength = 5.6f,
            alignmentExponent = 0.0f, 
            
            cohesionStrength = 4.0f,
            cohesionExponent = 0.0f,
            
            separationStrength = 120.0f,
            separationExponent = 1.0f,
            
            fearStrength = 140.0f,
            fearExponent = 1.0f,
            
            attackDistRange = 3f,
            attackAngleRange = Mathf.PI,
            
            attackMovementStrength = 20.1f,
            attackMovementExponent = 0.5f,
            
            emotionalState = 0f,
            morale = 1f,
            aggressionStrength = 10.4f,
            
            randomMovements = 6.0f,
        };
    }

    public override void Act()
    {
        Attack();
    }

    private void Attack() {
        if (target != null) {
            _p = target.GetPos() - GetPos();
            _v = target.GetVel() - GetVel();

            float t = FindTimeToImpact();
            Vector3 aimPos = _p + _v * t + 0.5f * _g * t * t;

            // Set spawn position of projectile
            Vector3 launchPos = GetPos();

            // Calc launch inclination
            float inclination = Inclination(aimPos);
            if (float.IsNaN(inclination)) { // Not possible to fire on target
                return;
            }
            
            // Set launch vector
            Vector3 launchVector = RemoveYComp(aimPos);
            launchVector.y = launchVector.magnitude * Mathf.Tan(inclination);
            launchVector = launchVector.normalized * _projSpeed;

            // Spawn and fire
            GameObject projectile = Instantiate(projectilePrefeb, launchPos, transform.rotation);
            Physics.IgnoreCollision(projectile.GetComponent<Collider>(), GetComponent<Collider>());
            projectile.GetComponent<RangedProjectile>().SetOwner(owner);
            projectile.GetComponent<RangedProjectile>().SetDamage(damage);
            projectile.GetComponent<Rigidbody>().AddForce(launchVector, ForceMode.VelocityChange);
        }
    }

    private float FindTimeToImpact() {
        float minSpeed = _projSpeed * Mathf.Cos(Mathf.PI / 4f);
        float maxDst = (minSpeed * minSpeed) / _g.magnitude;
        float maxTime = minSpeed / maxDst;

        float t = 0;
        float ft;
        float e = 0.1f;
        while (t < maxTime) { // Implement better solution for finding first non-negative root than linear search?
            ft = QuarticEquation(t);
            if (ft < e) {
                return t;
            } else {
                t += e;
            }
        }

        return 0; // No roots found, aim at current position
    }

    private float QuarticEquation(float t) {
        float a = 0.25f * Vector3.Dot(_g, _g);
        float b = Vector3.Dot(_v, _g);
        float c = Vector3.Dot(_p, _g) + Vector3.Dot(_v, _v) - _projSpeed * _projSpeed;
        float d = 2f * Vector3.Dot(_p, _v);
        float e = Vector3.Dot(_p, _p);

        return a * Mathf.Pow(t, 4) + b * Mathf.Pow(t, 3) + c * Mathf.Pow(t, 2) + d * t + e;
    }

    private float Inclination(Vector3 aimPos) {
        float g = _g.magnitude;
        float h = aimPos.y;
        float h2 = h * h;
        float x = RemoveYComp(aimPos).magnitude;
        float x2 = x * x;
        float v = _projSpeed;
        float v2 = v * v;
        float phase = Mathf.Atan2(x, h);
        float cos = (((g * x2) / v2) - h) / (Mathf.Sqrt(h2 + x2));
        float inclination = (Mathf.Acos(cos) + phase) / 2f;
        return (Mathf.PI / 2f) - inclination; // Pick the smaller solution
    }
}
