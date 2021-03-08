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

    private float _nextAttackTime;
    private float _nextBoostTime;
    private float _aimLockCompleteTime;
    private bool _aiming;

    // Start is called before the first frame update
    void Start() {
        base.Start();

        type = Type.Hero;
        cost = 100;
        health = maxHealth = 200;
        damage = 100;
        maxSpeed = 4f;
        targetHeight = 2f;
        collisionAvoidanceDistance = 3f;
        avoidCollisionWeight = 5f;
        hoverKi = 2f;
        hoverKp = 10f;
        timeBetweenActions = 5f;
        emotionalState = 0f;
        morale = moraleDefault = 1f;
        abilityDistance = 2f;

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
            attackAngleRange = 2f * Mathf.PI / 3f,

            approachMovementStrength = 20.1f,
            approachMovementExponent = 0.5f,

            aggressionStrength = 10.4f,

            randomMovements = 6.0f,
        };

        lockLaser.SetActive(false);
    }

    public override void Act() {
        if (target != null && _aiming == false) {
            Boid aimedTarget = target;
            _aiming = true;
            _aimLockCompleteTime = Time.time + aimLockTime;
            IEnumerator aimAndFire = AimAndFire(aimedTarget, laserDrawTime);
            StartCoroutine(aimAndFire);
        }
    }

    private IEnumerator AimAndFire(Boid target, float waitTime) {
        while (true) {
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
            enemy.GetComponent<Rigidbody>().AddExplosionForce(forcePower, position, damageRadius);
            float sqrDist = (enemy.GetPos() - position).sqrMagnitude;
            int takeDamage = (int) (damage / sqrDist);
            enemy.TakeDamage(takeDamage);
        }
    }

    private void SetLaser(Vector3 fromPos, Vector3 toPos, float width) {
        Vector3[] positions;
        positions = new Vector3[] {fromPos, toPos};
        LineRenderer laser = lockLaser.GetComponent<LineRenderer>();
        laser.SetPositions(positions);
        laser.startColor = owner.color;
        laser.startWidth = width;
        laser.endWidth = width;
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
