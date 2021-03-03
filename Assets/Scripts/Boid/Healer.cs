using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class Healer : Boid
{

    private float _healAmount;
    
    private Shader _attackAnimationShader;

    // Start is called before the first frame update
    void Start() {
        base.Start();
        
        _attackAnimationShader = Shader.Find("Sprites/Default");

        cost = 10;
        health = 100;
        damage = 1;
        maxSpeed = 4f;
        targetHeight = 2f;
        collisionAvoidanceDistance = 3f;
        avoidCollisionWeight = 5f;
        hoverKi = 2f;
        hoverKp = 10f;
        emotionalState = 0f;
        morale = 1f;
        
        timeBetweenActions = 0.5f;
        _healAmount = 10;
        
        classInfo = new ClassInfo {
            viewRadius = 3f,
            separationRadius = 0.3f,
            fearRadius = 1.3f,
            maxForce = 2.0f,
            
            alignmentStrength = 6.0f,
            alignmentExponent = 0.0f, 
            
            cohesionStrength = 6.0f,
            cohesionExponent = 0.0f,
            
            separationStrength = 120.0f,
            separationExponent = 1.0f,
            
            fearStrength = 100.0f,
            fearExponent = 0.5f,
            
            // Negative attack distance implies that healing distance should be used instead
            attackDistRange = -1f,
            attackAngleRange = Mathf.PI / 4.0f,
            
            healDistRange = 3f,
            
            approachMovementStrength = 20.0f,
            approachMovementExponent = 0.5f,
            
            aggressionStrength = 8.4f,
            
            randomMovements = 4.0f,
        };
    }

    public override void Act()
    {
        // Healers cannot attack
        Heal();
    }

    private void Heal()
    {
        if (target == null) return;
        
        //target.        
    }

    /*public override void Attack() {
        if (target != null && Time.time > _nextAttackTime) {
            _nextAttackTime = Time.time + timeBetweenAttacks;
            target.TakeDamage(damage);
            AnimateAttack(this.GetPos(), target.GetPos());
        }
    }

    private void AnimateAttack(Vector3 fromPos, Vector3 toPos) {
        LineRenderer lineRenderer = new GameObject("Line").AddComponent<LineRenderer>();
        lineRenderer.material = new Material(_attackAnimationShader);
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.startColor = owner.color;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPosition(0, fromPos);
        lineRenderer.SetPosition(1, toPos);
        Destroy(lineRenderer, 0.1f);
    }*/
}
