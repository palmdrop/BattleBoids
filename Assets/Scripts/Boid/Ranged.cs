using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Numerics;

public class Ranged : Boid
{

    [SerializeField] private GameObject projectilePrefeb;
    [SerializeField] private AudioClip rangedFireAudio;
    [Range(0f, 1f)] public float rangedFireAudioVolume;

    private float _projSpeed = 8;

    private UnityEngine.Vector3 _p;
    private UnityEngine.Vector3 _v;
    private UnityEngine.Vector3 _g = Physics.gravity;

    // Start is called before the first frame update
    void Start()
    {
        base.Start();

        type = Type.Ranged;
        Cost = 15;
        health = maxHealth = 75;
        damage = 10;
        boostedDamage = 50;
        maxSpeed = 7f;
        avoidCollisionWeight = 5f;
        timeBetweenActions = 1f;
        emotionalState = 0f;
        morale = moraleDefault = 1f;
        abilityDistance = 0;
        meshDefaultLayer = LayerMask.NameToLayer("OutlineWhite");

        ClassInfos.infos[(int)type] = new ClassInfo
        {
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

            attackDistRange = 4f,
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

    protected override bool Act()
    {
        return Attack();
    }

    private bool Attack()
    {
        if (HasTarget() && !target.IsDead())
        {
            
            float3 velOffset = target.GetVel() - GetVel();
            float3 aimPos;

            //Get the time to the target. If time is given all other parameters are set.
            float t1 = CalculateTime(transform.position, target.transform.position, velOffset, _g.y, _projSpeed);

            //May yield complex or negative results, so discard and don't shoot in that case.
            if (t1 < 0)
            {
                return false;
            }


            aimPos = (float3)target.transform.position + velOffset * t1;
            //Simple calculation to get the horizontal firing angle.
            float theta = CalculateAngle(transform.position, aimPos, -_g.magnitude, _projSpeed);

            //Make sure it's not firing backwards.
            if (theta < -math.PI / 2 || theta > math.PI / 2)
                return false;
            float3 offset = aimPos - (float3)transform.position;


            GameObject projectile = ProjectilePoolManager.SharedInstance.getPooledObject();
            if (projectile != null)
            {
                projectile.gameObject.SetActive(true);
                RangedProjectile p = projectile.GetComponent<RangedProjectile>();
                p.Fire(_projSpeed, transform.position, theta, new UnityEngine.Vector3(offset.x, 0, offset.z).normalized, GetVel());
                p.SetOwner(owner);
                p.SetDamage(IsBoosted() ? boostedDamage : damage);
                p.SetColor();
            }

            AudioManager.instance.PlaySoundEffectAtPoint(rangedFireAudio, GetPos(), rangedFireAudioVolume);
            return true;
        }
        return false;
    }


    float CalculateTime(float3 origin, float3 target, float3 targetVel, float g, float v0)
    {
        //In reality a quartic equation solver using ferarri's method.
        //First some constant calculations to fit typical formula. Se paper for more information.
        float px = target.x - origin.x;
        float py = target.z - origin.z;
        float pz = target.y - origin.y;
        float vx = targetVel.x;
        float vy = targetVel.z;
        float vz = targetVel.y;
        g = -g;

        float a = sqr(g) / 4;
        float b = (vz * g);
        float c = (pz * g + sqr(vz) - sqr(v0) + sqr(vy) + sqr(vx));
        float d = (2 * pz * vz + 2 * py * vy + 2 * px * vx);
        float e = (sqr(pz) + sqr(py) + sqr(px));

        //We now have the proper constants. As g is constant and not zero this won't be a problem,
        //but typically this check is needed so as to not divide by zero.
        if (math.abs(a) < 0.1f)
            if (a == 0f)
                a = 0.1f;
            else
                a = math.sign(a) * 0.1f;

        float alpha =
            -(3 * sqr(b)) / (8 * sqr(a))
            + c / a;

        float beta =
            +cube(b) / (8 * cube(a))
            - (b * c) / (2 * sqr(a))
            + d / a;

        float gamma =
            -(3 * quadr(b)) / (256 * quadr(a))
            + (c * sqr(b)) / (16 * cube(a))
            - (b * d) / (4 * sqr(a))
            + e / a;

        float p =
            -(sqr(alpha) / 12)
            - gamma;

        float q =
            -(cube(alpha) / 108)
            + (alpha * gamma) / 3
            - sqr(beta) / 8;

        //We need to go into the complex plane here. Not sure as to why but some real solutions require this.
        Complex r = (-q / 2) + Complex.Sqrt((sqr(q)) / 4 + (cube(p)) / 27);
        Complex u = Complex.Pow(r, 1.0f / 3.0f);

        Complex y;
        if (Complex.Abs(u) < 0.001f)
            y = -(5f / 6f) * alpha + u - Complex.Pow(q, 1.0f / 3.0f);
        else
            y = -(5f / 6f) * alpha + u - p / (3 * u);
        Complex w = Complex.Sqrt(alpha + 2 * y);

        double tBest = double.MaxValue;

        for (int i = 0; i < 4; i++)
        {
            Complex tmp = -b / (4 * a) + ((i / 2) * 2 - 1) * 1f / 2 * w + ((i % 2) * 2 - 1) * 1f / 2 * Complex.Sqrt(-(3 * alpha + 2 * y + ((2 * beta / w) * ((i / 2) * 2 - 1))));
            //Floating point errors may have accumulated in the imaginary part, so we check if we have something close to a real solution.
            if (math.abs(tmp.Imaginary) < 0.05d)
            {
                if (tmp.Real != System.Double.NaN && tmp.Real > 0 && tmp.Real < tBest)
                    tBest = tmp.Real;
            }
        }
        return (float)tBest;

    }

    float sqr(float v)
    {
        return v * v;
    }
    float cube(float v)
    {
        return v * v * v;
    }
    float quadr(float v)
    {
        return v * v * v * v;
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
