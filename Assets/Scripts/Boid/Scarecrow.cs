using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class Scarecrow : Boid {

    private float _nextAttackTime;

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
        timeBetweenAttacks = 1f;
        dead = false;
        //mesh = ;
        collisionMask = LayerMask.GetMask("Wall", "Obstacle");
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
            
            attackDistRange = 2f,
            attackAngleRange = Mathf.PI,
            
            attackMovementStrength = 20.1f,
            attackMovementExponent = 0.5f,
            
            aggressionStrength = 10.4f,
            
            randomMovements = 6.0f,
        };

        SetColor();
    }

    public override void Attack() {
        if (target != null && Time.time > _nextAttackTime) {
            _nextAttackTime = Time.time + timeBetweenAttacks;
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