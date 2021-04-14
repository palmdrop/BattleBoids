using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Ranged : Boid {

    [SerializeField] private GameObject projectilePrefeb;
    [SerializeField] private AudioClip rangedFireAudio;
    [Range(0f, 1f)] public float rangedFireAudioVolume;

    private float _projSpeed = 8;

    private Vector3 _p;
    private Vector3 _v;
    private Vector3 _g = Physics.gravity;

    // Start is called before the first frame update
    void Start() {
        base.Start();

        type = Type.Ranged;
        Cost = 15;
        health = maxHealth = 60;
        damage = 20;
        boostedDamage = 50;
        maxSpeed = 7f;
        avoidCollisionWeight = 5f;
        timeBetweenActions = 1f;
        emotionalState = 0f;
        morale = moraleDefault = 1f;
        abilityDistance = 0;
        meshDefaultLayer = LayerMask.NameToLayer("OutlineWhite");

        ClassInfos.infos[(int)type] = new ClassInfo {
            type = this.type,
            viewRadius = 1.6f,
            separationRadius = 0.38f,
            fearRadius = 1.3f,
            maxForce = 6.5f,
            
            accelerationDesire = 0.1f,

            maxHealth = this.maxHealth,
            collisionAvoidanceDistance = 3f,
            collisionMask = (uint)this.collisionMask.value,
            groundMask = (uint)this.groundMask.value,
            abilityDistance = this.abilityDistance,

            alignmentStrength = 2.5f,
            alignmentExponent = 0.0f, 
            
            cohesionStrength = 3.0f,
            cohesionExponent = 0.0f,
            
            separationStrength = 120.0f,
            separationExponent = 1.0f,

            gravity = 1f,
            
            fearStrength = 20.0f,
            fearExponent = 1.0f,

            attackDistRange = 3f,
            attackAngleRange = Mathf.PI,
            
            approachMovementStrength = 0.3f,
            approachMovementExponent = 0.5f,
            
            aggressionStrength = 3.4f,
            aggressionFalloff = 0.2f,
            aggressionDistanceCap = 10.0f,
            maxAggressionMultiplier = 2.2f,

            avoidCollisionWeight = 1000f,

            searchStrength = 5.4f,

            avoidanceStrength = 20.0f,
            
            randomMovements = 0.5f,

            hoverKi = 2f,
            hoverKp = 10f,
            targetHeight = 2f,

            colliderRadius = GetComponent<SphereCollider>().radius
        };
    }

    protected override void Act()
    {
        Attack();
    }

    private void Attack() {
        if (HasTarget() && !target.IsDead()) {
            _p = target.GetPos() - GetPos();
            _v = target.GetVel() - GetVel();

            float t = FindTimeToImpact();
            Vector3 aimPos = _p + _v * t + _g * (0.5f * t * t);

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

            // Spawn and fire - old code
            /*GameObject projectile = Instantiate(projectilePrefeb, launchPos, transform.rotation);
            Physics.IgnoreCollision(projectile.GetComponent<Collider>(), GetComponent<Collider>());
            projectile.GetComponent<RangedProjectile>().SetOwner(owner);
            projectile.GetComponent<RangedProjectile>().SetDamage(IsBoosted() ? boostedDamage : damage);
            projectile.GetComponent<Rigidbody>().AddForce(launchVector, ForceMode.VelocityChange);*/

            GameObject projectile = ProjectilePoolManager.SharedInstance.getPooledObject();
            if (projectile != null)
            {
                projectile.transform.position = launchPos;
                projectile.transform.rotation = transform.rotation;
                projectile.gameObject.SetActive(true);
                //Physics.IgnoreCollision(projectile.GetComponent<Collider>(), GetComponent<Collider>());
                RangedProjectile p = projectile.GetComponent<RangedProjectile>();
                p.SetOwner(owner);
                p.SetDamage(IsBoosted() ? boostedDamage : damage);
                //Rigidbody body = projectile.GetComponent<Rigidbody>();
                //body.velocity = new Vector3(0,0,0);
                //body.angularVelocity = new Vector3(0,0,0);
                //body.AddForce(launchVector, ForceMode.VelocityChange);
                p.SetForce(launchVector);
                p.SetColor();
            }

            AudioManager.instance.PlaySoundEffectAtPoint(rangedFireAudio, GetPos(), rangedFireAudioVolume);
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
