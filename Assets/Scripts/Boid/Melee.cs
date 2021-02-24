using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Melee : Boid {

    private float _nextAttackTime;

    // Start is called before the first frame update
    void Start() {
        base.Start();
        
        cost = 10;
        health = 100;
        damage = 10;
        maxSpeed = 2f;
        targetHeight = 2f;
        collisionAvoidanceDistance = 3f;
        avoidCollisionWeight = 5f;
        hover_Ki = 2f;
        hover_Kp = 10f;
        timeBetweenAttacks = 0.1f;
        dead = false;
        //mesh = ;
        collisionMask = LayerMask.GetMask("Wall", "Obstacle");

        classInfo = new ClassInfo {
            viewRadius = 0.5f,
            attackDstRange = 0.5f,
            attackAngleRange = 45,
            alignmentStrength = 0.7f,
            alignmentExponent = 1.0f, 
            cohesionStrength = 1.5f,
            cohesionExponent = 0.8f,
            separationStrength = 0.5f,
            separationExponent = 10.0f,
            fearStrength = 4.65f,
            fearExponent = 8.0f,
            attackMovementStrength = 1.1f,
            attackMovementExponent = 5.0f,
            emotionalState = 0f,
            morale = 1f,
            aggressionStrength = 2.0f,
            randomMovements = 6.0f,
        };
    }

    public override void Attack() {
        if (_target != null && Time.time > _nextAttackTime) {
            _nextAttackTime = Time.time + timeBetweenAttacks;
            _target.TakeDamage(damage);
            AnimateAttack(this.GetPos(), _target.GetPos());
        }
    }

    private void AnimateAttack(Vector3 fromPos, Vector3 toPos) {
        LineRenderer lineRenderer = new GameObject("Line").AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPosition(0, fromPos);
        lineRenderer.SetPosition(1, toPos);
        Destroy(lineRenderer, 0.2f);
    }
}
