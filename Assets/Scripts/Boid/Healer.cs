using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class Healer : Boid
{
    private int _healAmount;
    private static Shader _healAnimationShader;

    // Start is called before the first frame update
    void Start() {
        base.Start();
        
        _healAnimationShader = Shader.Find("Sprites/Default");

        cost = 20;
        health = maxHealth = 100;
        damage = 0;
        maxSpeed = 4f;
        targetHeight = 2f;
        collisionAvoidanceDistance = 3f;
        avoidCollisionWeight = 5f;
        hoverKi = 2f;
        hoverKp = 10f;
        emotionalState = 0f;
        morale = 1f;
        
        timeBetweenActions = 0.2f;
        _healAmount = 4;
        
        classInfo = new ClassInfo {
            viewRadius = 3f,
            separationRadius = 0.3f,
            fearRadius = 1.2f,
            maxForce = 2.0f,
            healRadius = 1f,
            
            alignmentStrength = 6.0f,
            alignmentExponent = 0.0f, 
            
            cohesionStrength = 10.0f,
            cohesionExponent = 0.0f,
            
            separationStrength = 120.0f,
            separationExponent = 1.0f,
            
            fearStrength = 200.0f,
            fearExponent = 1.5f,
            
            // Negative attack distance implies that healing distance should be used instead
            attackDistRange = -1f,
            attackAngleRange = Mathf.PI / 4.0f,
            
            approachMovementStrength = 35.0f,
            approachMovementExponent = 0.5f,
            
            aggressionStrength = 8.4f,
            
            randomMovements = 4.0f,
        };
    }

    protected override void Act()
    {
        // Healers cannot attack
        Heal();
    }

    private void Heal()
    {
        // Do not heal if there is no target or if the target has max health
        if (target == null || target.GetHealth() == target.GetMaxHealth()) return;
        
        // Do not heal if target is out of range
        if (math.distancesq(this.GetPos(), target.GetPos()) > classInfo.healRadius * classInfo.healRadius) return;
        
        target.ReceiveHealth(_healAmount);
        AnimateHeal(this.GetPos(), target.GetPos());
    }

    /*public override void Attack() {
        if (target != null && Time.time > _nextAttackTime) {
            _nextAttackTime = Time.time + timeBetweenAttacks;
            target.TakeDamage(damage);
            AnimateAttack(this.GetPos(), target.GetPos());
        }
    }*/

    private void AnimateHeal(Vector3 fromPos, Vector3 toPos) {
        LineRenderer lineRenderer = new GameObject("Line").AddComponent<LineRenderer>();
        lineRenderer.material = new Material(_healAnimationShader);
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.startColor = new Color(0.5f, 0.5f, 0.0f, 0.5f);
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPosition(0, fromPos);
        lineRenderer.SetPosition(1, toPos);
        Destroy(lineRenderer, 0.1f);
    }
}
