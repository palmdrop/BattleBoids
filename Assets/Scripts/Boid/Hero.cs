using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class Hero : Boid {

    [SerializeField] private GameObject lockLaser;
    [SerializeField] private float laserDoneWidth;
    [SerializeField] private GameObject hitAnimationPrefab;
    [SerializeField] private float aimLockTime;
    [SerializeField] private float laserDrawTime;
    [SerializeField] private float damageRadius;
    [SerializeField] private float forcePower;
    [SerializeField] private float boostTime;

    private LineRenderer _lockLaserRenderer;

    //private float _nextBoostTime;
    private float _aimLockCompleteTime;
    private bool _aiming;

    // Start is called before the first frame update
    void Start() {
        base.Start();

        type = Type.Hero;
        Cost = 100;
        health = maxHealth = 200;
        damage = 100;
        maxSpeed = 6f;
        avoidCollisionWeight = 5f;
        timeBetweenActions = 0.1f;
        emotionalState = 0f;
        morale = moraleDefault = 1f;
        abilityDistance = 2f;

        ClassInfos.infos[(int)type] = new ClassInfo {
            type = this.type,
            viewRadius = 1f,
            separationRadius = 0.42f,
            fearRadius = 0.5f,
            maxForce = 6.0f,
            
            accelerationDesire = 0.1f,

            maxHealth = this.maxHealth,
            collisionAvoidanceDistance = 3f,
            collisionMask = (uint)this.collisionMask.value,
            groundMask = (uint)this.groundMask.value,
            abilityDistance = this.abilityDistance,

            alignmentStrength = 4.0f,
            alignmentExponent = 0.0f, 

            cohesionStrength = 3.0f,
            cohesionExponent = 0.0f,

            separationStrength = 220.0f,
            separationExponent = 1.0f,

            fearStrength = 5.0f,
            fearExponent = 1.1f,

            gravity = 1f,

            attackDistRange = 3f,
            attackAngleRange = 2f * Mathf.PI / 3f,

            approachMovementStrength = 14.1f,
            approachMovementExponent = 1.0f,

            aggressionStrength = 3.4f,
            aggressionFalloff = 0.8f,
            aggressionDistanceCap = 10.0f,
            maxAggressionMultiplier = 2.2f,

            searchStrength = 6.4f,

            avoidanceStrength = 30f,

            avoidCollisionWeight = 1000f,

            randomMovements = 2.0f,

            hoverKi = 2f,
            hoverKp = 10f,
            targetHeight = 2f
        };

        _lockLaserRenderer = lockLaser.GetComponent<LineRenderer>();
        lockLaser.SetActive(false);
        SetLaserColor();
    }

    protected override void Act() {
        if (HasTarget() && !target.IsDead() && _aiming == false) {
            _aiming = true;
            _aimLockCompleteTime = Time.time + aimLockTime;
            IEnumerator aimAndFire = AimAndFire(target, laserDrawTime);
            StartCoroutine(aimAndFire);
        }
        if (HasFriendlyTarget() && !friendlyTarget.IsDead()) {
            friendlyTarget.GiveBoost(boostTime);
        }
    }

    private IEnumerator AimAndFire(Boid target, float waitTime) {
        while (!IsDead() && !target.IsDead()) {
            Vector3 targetPos = target.GetPos();
            float width = laserDoneWidth
                        - laserDoneWidth
                        * (_aimLockCompleteTime - Time.time) / aimLockTime;
            SetLaser(GetPos(), targetPos, width);
            lockLaser.SetActive(true);

            if (Time.time > _aimLockCompleteTime) { // Aiming done, fire
                Fire(targetPos);
                break;
            } else if (NotReachable(targetPos)) { // Target moved away, reset
                break;
            } else { // Continue aiming
                yield return new WaitForSeconds(waitTime);
            }
        }
        _aiming = false;
        lockLaser.SetActive(false);
    }

    private void Fire(Vector3 position) {
        // Spawn explosion and set color
        GameObject hitAnimation = Instantiate(hitAnimationPrefab, position, transform.rotation);
        ParticleSystem.MainModule psMain;
        foreach (Transform child in hitAnimation.transform) {
            psMain = child.GetComponent<ParticleSystem>().main;
            psMain.startColor = owner.color;
        }
        Destroy(hitAnimation, hitAnimation.transform.GetChild(0).GetComponent<ParticleSystem>().main.duration);

        // Set explosion forces and damage
        List<Boid> enemies = FindEnemiesInSphere(position, damageRadius, LayerMask.GetMask("Units"));
        foreach (Boid enemy in enemies) {
            enemy.GetRigidbody().AddExplosionForce(forcePower, position, damageRadius);
            float sqrDist = (enemy.GetPos() - position).sqrMagnitude;
            int takeDamage = (int) (damage / sqrDist);
            enemy.TakeDamage(takeDamage);
        }
    }

    private void SetLaserColor() {
        Color start = new Color(owner.color.r, owner.color.g, owner.color.b, 0.25f);
        Color end = new Color(owner.color.r, owner.color.g, owner.color.b, 0f);
        _lockLaserRenderer.SetColors(start, end);
    }

    private void SetLaser(Vector3 fromPos, Vector3 toPos, float width) {
        Vector3[] positions = new Vector3[] {fromPos, toPos};
        _lockLaserRenderer.SetPositions(positions);
        _lockLaserRenderer.startWidth = width;
        _lockLaserRenderer.endWidth = width;
    }

    private bool NotReachable(Vector3 pos) {
        Vector3 vector = pos - GetPos();
        float dist = vector.magnitude;
        float angle = math.acos(math.dot(vector, transform.forward) / dist);

        if (dist > ClassInfos.infos[(int) type].attackDistRange || angle > ClassInfos.infos[(int) type].attackAngleRange) {
            return true;
        }
        return false;
    }
}
