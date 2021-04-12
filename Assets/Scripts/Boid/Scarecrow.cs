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
        maxSpeed = 6f;
        avoidCollisionWeight = 5f;
        timeBetweenActions = 1f;
        morale = moraleDefault = 1f;
        abilityDistance = 2f;

        ClassInfos.infos[(int)type] = new ClassInfo {
            type = this.type,
            viewRadius = 1f,
            separationRadius = 0.4f,
            fearRadius = 0.5f,
            maxForce = 7.0f,
            
            accelerationDesire = 0.1f,

            maxHealth = this.maxHealth,
            collisionAvoidanceDistance = 3f,
            collisionMask = (uint)this.collisionMask.value,
            groundMask = (uint)this.groundMask.value,
            abilityDistance = this.abilityDistance,

            alignmentStrength = 2.5f,
            alignmentExponent = 0.0f, 
            
            cohesionStrength = 3.0f,
            cohesionExponent = 0.0f,
            
            separationStrength = 220.0f,
            separationExponent = 1.0f,
            
            gravity = 1f,
            
            fearStrength = 7.0f,
            fearExponent = 1.0f,
            
            attackDistRange = 2f,
            attackAngleRange = Mathf.PI,
            
            approachMovementStrength = 0.5f,
            approachMovementExponent = 1.0f,
            
            aggressionStrength = 3.4f,
            aggressionFalloff = 0.3f,
            aggressionDistanceCap = 10.0f,
            maxAggressionMultiplier = 2.2f,

            avoidCollisionWeight = 1000f,

            searchStrength = 5.4f,

            avoidanceStrength = 10.0f,
            
            randomMovements = 0.5f,

            hoverKi = 2f,
            hoverKp = 10f,
            targetHeight = 2f,

            fearMultiplier = 100f
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
