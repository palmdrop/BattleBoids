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
        Cost = 50;
        health = maxHealth = 100;
        damage = 1;
        maxSpeed = 3f;
        avoidCollisionWeight = 5f;
        timeBetweenActions = 1f;
        morale = moraleDefault = 1f;
        abilityDistance = 2f;

        ClassInfos.infos[(int)type] = new ClassInfo {
            type = this.type,
            viewRadius = 1f,
            separationRadius = 0.3f,
            fearRadius = 1.0f,
            maxForce = 2f,

            maxHealth = this.maxHealth,
            collisionAvoidanceDistance = 3f,
            collisionMask = (uint)this.collisionMask.value,
            groundMask = (uint)this.groundMask.value,
            abilityDistance = this.abilityDistance,

            confidenceThreshold = 0.4f,
            
            alignmentStrength = 5.6f,
            alignmentExponent = 0.0f, 
            
            cohesionStrength = 6.0f,
            cohesionExponent = 0.0f,
            
            separationStrength = 120.0f,
            separationExponent = 1.0f,
            
            gravity = 1f,
            
            fearStrength = 60.0f,
            fearExponent = 2.0f,
            
            attackDistRange = 2f,
            attackAngleRange = Mathf.PI,
            
            approachMovementStrength = 20.1f,
            approachMovementExponent = 0.5f,
            
            aggressionStrength = 5.4f,
            aggressionFalloff = 2.0f,
            aggressionDistanceCap = 10.0f,
            maxAggressionMultiplier = 1.8f,

            avoidCollisionWeight = 100f,

            searchStrength = 10.4f,
            
            avoidanceStrength = 30.0f,
            
            randomMovements = 6.0f,

            hoverKi = 2f,
            hoverKp = 10f,
            targetHeight = 2f,

            fearMultiplier = 10f
        };

        SetColor();
    }

    protected override void Act() {
    }

    private void SetColor() {
        ParticleSystem.MainModule psMain = GetComponent<ParticleSystem>().main;
        psMain.startColor = owner.color;
    }
}
