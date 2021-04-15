using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Ranged : Boid {

    [SerializeField] private GameObject projectilePrefeb;
    [SerializeField] private AudioClip rangedFireAudio;
    [Range(0f, 1f)] public float rangedFireAudioVolume;

    private float _projSpeed = 8;
    private float _eps = 0.01f;

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
        maxSpeed = 4f;
        avoidCollisionWeight = 5f;
        timeBetweenActions = 1f;
        emotionalState = 0f;
        morale = moraleDefault = 1f;
        abilityDistance = 0;
        meshDefaultLayer = LayerMask.NameToLayer("OutlineWhite");

        ClassInfos.infos[(int)type] = new ClassInfo {
            type = this.type,
            viewRadius = 1.6f,
            separationRadius = 0.35f,
            fearRadius = 1.6f,
            maxForce = 6.5f,

            maxHealth = this.maxHealth,
            collisionAvoidanceDistance = 3f,
            collisionMask = (uint)this.collisionMask.value,
            groundMask = (uint)this.groundMask.value,
            abilityDistance = this.abilityDistance,

            confidenceThreshold = 3.0f,
            
            alignmentStrength = 3.6f,
            alignmentExponent = 0.0f, 
            
            cohesionStrength = 2.0f,
            cohesionExponent = 0.0f,

            separationStrength = 120.0f,
            separationExponent = 1.0f,

            gravity = 1f,
            
            fearStrength = 10.0f,
            fearExponent = 2.0f,

            attackDistRange = 3f,
            attackAngleRange = Mathf.PI,

            approachMovementStrength = 20.1f,
            approachMovementExponent = 0.5f,
            
            aggressionStrength = 3.4f,
            aggressionFalloff = 2.0f,
            aggressionDistanceCap = 10.0f,
            maxAggressionMultiplier = 1.8f,

            avoidCollisionWeight = 1000f,

            searchStrength = 10.4f,

            avoidanceStrength = 70.0f,
            
            randomMovements = 3.0f,

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
            /*_p = target.GetPos() - GetPos();
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
            launchVector = launchVector.normalized * _projSpeed;*/

            // Spawn and fire - old code
            /*GameObject projectile = Instantiate(projectilePrefeb, launchPos, transform.rotation);
            Physics.IgnoreCollision(projectile.GetComponent<Collider>(), GetComponent<Collider>());
            projectile.GetComponent<RangedProjectile>().SetOwner(owner);
            projectile.GetComponent<RangedProjectile>().SetDamage(IsBoosted() ? boostedDamage : damage);
            projectile.GetComponent<Rigidbody>().AddForce(launchVector, ForceMode.VelocityChange);*/


            //float3 vel = new Vector3(2, 0, 0);
            //float3 originVelocity = new Vector3(2, 0, 0);
            float3 velOffset = target.GetVel() - GetVel();
            float3 oldPos;
            float3 newPos = target.transform.position;
            do
            {
                float t1 = CalculateTime(transform.position, newPos, -_g.magnitude, _projSpeed);
                if (t1 < 0)
                    return;
                oldPos = newPos;
                newPos = (float3)target.transform.position + velOffset * t1;
            } while (math.length(newPos - oldPos) > _eps);



            float theta = CalculateAngle(transform.position, newPos, -_g.magnitude, _projSpeed);
            if (theta < -math.PI / 2 || theta > math.PI / 2)
                return;
            float3 offset = newPos - (float3)transform.position;

            //p.Fire(_projSpeed, transform.position, theta, new Vector3(offset.x, 0, offset.z).normalized, GetVel());



            GameObject projectile = ProjectilePoolManager.SharedInstance.getPooledObject();
            if (projectile != null)
            {
                projectile.gameObject.SetActive(true);
                //Physics.IgnoreCollision(projectile.GetComponent<Collider>(), GetComponent<Collider>());
                RangedProjectile p = projectile.GetComponent<RangedProjectile>();
                p.Fire(_projSpeed, transform.position, theta, new Vector3(offset.x, 0, offset.z).normalized, GetVel());
                p.SetOwner(owner);
                p.SetDamage(IsBoosted() ? boostedDamage : damage);
                //Rigidbody body = projectile.GetComponent<Rigidbody>();
                //body.velocity = new Vector3(0,0,0);
                //body.angularVelocity = new Vector3(0,0,0);
                //body.AddForce(launchVector, ForceMode.VelocityChange);
                p.SetColor();
            }

            AudioManager.instance.PlaySoundEffectAtPoint(rangedFireAudio, GetPos(), rangedFireAudioVolume);
        }
    }


    float CalculateTime(float3 origin, float3 target, float g, float v0)
    {
        float3 m = target - origin;
        float2 Mxz = new float2(m.x, m.z);
        float Mx = math.sqrt(math.dot(Mxz, Mxz));
        float My = m.y;

        float a = -(Mx * Mx * g * g) + 2 * My * v0 * v0 * g + v0 * v0 * v0 * v0;
        if (a < 0)
            return -1;
        float b = math.sqrt(a) / (g * g);
        float c = My / g + (v0 * v0) / (g * g);
        if (-b + c > 0)
            return math.SQRT2 * math.sqrt(-b + c);
        else if (b + c > 0)
            return math.SQRT2 * math.sqrt(b + c);
        else
            return -1;
    }

    float CalculateAngle(float3 origin, float3 target, float g, float v0)
    {
        float3 m = target - origin;
        float2 Mxz = new float2(m.x, m.z);
        float Mx = math.sqrt(math.dot(Mxz, Mxz));
        float My = m.y;
        float t1;

        float a = -(Mx * Mx * g * g) + 2 * My * v0 * v0 * g + v0 * v0 * v0 * v0;
        if (a < 0)
            return math.PI;
        float b = math.sqrt(a) / (g * g);
        float c = My / g + (v0 * v0) / (g * g);
        if (-b + c > 0)
            t1 = math.SQRT2 * math.sqrt(-b + c);
        else if (b + c > 0)
            t1 = math.SQRT2 * math.sqrt(b + c);
        else
            return math.PI;
        return math.asin((My - (g * t1 * t1) / 2) / (t1 * v0));
    }
}
