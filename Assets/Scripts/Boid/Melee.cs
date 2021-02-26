using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class Melee : Boid {

    private float _nextAttackTime;

    // Start is called before the first frame update
    void Start() {
        base.Start();

        cost = 10;
        health = maxHealth = 100;
        damage = 1;
        maxSpeed = 4f;
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
            
            attackDstRange = 1f,
            attackAngleRange = Mathf.PI / 4.0f,
            
            attackMovementStrength = 20.1f,
            attackMovementExponent = 0.5f,
            
            emotionalState = 0f,
            morale = 1f,
            aggressionStrength = 10.4f,
            
            randomMovements = 6.0f,
        };
    }

    public override void Attack() {
        if (target != null && Time.time > _nextAttackTime) {
            _nextAttackTime = Time.time + timeBetweenAttacks;
            target.TakeDamage(damage);
            AnimateAttack(this.GetPos(), target.GetPos());
        }
    }

    private void AnimateAttack(Vector3 fromPos, Vector3 toPos) {
        LineRenderer lineRenderer = new GameObject("Line").AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.startColor = owner.color;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPosition(0, fromPos);
        lineRenderer.SetPosition(1, toPos);
        Destroy(lineRenderer, 0.1f);
    }
}
