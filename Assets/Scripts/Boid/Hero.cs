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
        maxSpeed = 4f;
        avoidCollisionWeight = 5f;
        timeBetweenActions = 0.1f;
        emotionalState = 0f;
        morale = moraleDefault = 1f;
        abilityDistance = 2f;

        ClassInfos.infos[(int)type] = new ClassInfo {
            type = this.type,
            viewRadius = 3f,
            separationRadius = 0.3f,
            fearRadius = 1.0f,
            maxForce = 2f,

            maxHealth = this.maxHealth,
            collisionAvoidanceDistance = 3f,
            collisionMask = (uint)this.collisionMask.value,
            groundMask = (uint)this.groundMask.value,
            abilityDistance = this.abilityDistance,

            confidenceThreshold = 0.5f,

            alignmentStrength = 5.6f,
            alignmentExponent = 0.0f, 

            cohesionStrength = 4.0f,
            cohesionExponent = 0.0f,

            separationStrength = 120.0f,
            separationExponent = 1.0f,

            fearStrength = 140.0f,
            fearExponent = 1.0f,

            gravity = 1f,

            attackDistRange = 3f,
            attackAngleRange = 2f * Mathf.PI / 3f,

            approachMovementStrength = 20.1f,
            approachMovementExponent = 0.5f,

            aggressionStrength = 4.4f,
            searchStrength = 10.4f,

            avoidCollisionWeight = 100f,

            randomMovements = 6.0f,

            hoverKi = 2f,
            hoverKp = 10f,
            targetHeight = 2f
        };

        _lockLaserRenderer = lockLaser.GetComponent<LineRenderer>();
        lockLaser.SetActive(false);
        SetLaserColor();
    }

    protected override void Act() {
        if (HasTarget() && _aiming == false) {
            Boid aimedTarget = target;
            _aiming = true;
            _aimLockCompleteTime = Time.time + aimLockTime;
            IEnumerator aimAndFire = AimAndFire(aimedTarget, laserDrawTime);
            StartCoroutine(aimAndFire);
        }
        if (HasFriendlyTarget()) {
            friendlyTarget.GiveBoost(boostTime);
        }
    }

    private IEnumerator AimAndFire(Boid target, float waitTime) {
        while (!IsDead()) {
            Vector3 targetPos = target.GetPos();
            float width = laserDoneWidth
                        - laserDoneWidth
                        * (_aimLockCompleteTime - Time.time) / aimLockTime;
            SetLaser(GetPos(), targetPos, width);
            lockLaser.SetActive(true);

            if (Time.time > _aimLockCompleteTime) { // Aiming done, fire
                _aiming = false;
                lockLaser.SetActive(false);
                Fire(targetPos);
                yield break;
            } else if (NotReachable(target.GetPos())) { // Target moved away, reset
                _aiming = false;
                lockLaser.SetActive(false);
                yield break;
            } else { // Continue aiming
                yield return new WaitForSeconds(waitTime);
            }
        }
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

        if (dist > classInfo.attackDistRange || angle > classInfo.attackAngleRange) {
            return true;
        }
        return false;
    }
}
