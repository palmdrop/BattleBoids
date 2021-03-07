using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class Scarecrow : Boid {

    // Start is called before the first frame update
    void Start() {
        base.Start();

        type = Type.Scarecrow;
        cost = 50;
        health = maxHealth = 100;
        damage = 1;
        maxSpeed = 3f;
        targetHeight = 2f;
        collisionAvoidanceDistance = 3f;
        avoidCollisionWeight = 5f;
        hoverKi = 2f;
        hoverKp = 10f;
        timeBetweenActions = 1f;
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
            
            gravity = 1f,
            
            fearStrength = 140.0f,
            fearExponent = 1.0f,
            
            attackDistRange = 2f,
            attackAngleRange = Mathf.PI,
            
            approachMovementStrength = 20.1f,
            approachMovementExponent = 0.5f,
            
            aggressionStrength = 10.4f,
            
            randomMovements = 6.0f,
        };

        SetColor();
    }

    public override void Act() {
        if (target != null) {
            List<Boid> enemies = FindEnemiesInSphere(GetPos(), classInfo.attackDistRange, LayerMask.GetMask("Units"));
            foreach (Boid enemy in enemies) {
                enemy.TakeDamage(damage);
            }
        }
    }

    private void SetColor() {
        ParticleSystem.MainModule psMain = GetComponent<ParticleSystem>().main;
        psMain.startColor = owner.color;
    }
}
