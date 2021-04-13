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
        meshDefaultLayer = LayerMask.NameToLayer("OutlineWhite");

        ClassInfos.infos[(int)type] = new ClassInfo {
            type = this.type,
            viewRadius = 1f,
            separationRadius = 0.5f,
            fearRadius = 1.0f,
            maxForce = 6.5f,

            maxHealth = this.maxHealth,
            collisionAvoidanceDistance = 3f,
            collisionMask = (uint)this.collisionMask.value,
            groundMask = (uint)this.groundMask.value,
            abilityDistance = this.abilityDistance,

            confidenceThreshold = 0.4f,
            
            alignmentStrength = 3.6f,
            alignmentExponent = 0.0f, 
            
            cohesionStrength = 2.0f,
            cohesionExponent = 0.0f,
            
            separationStrength = 120.0f,
            separationExponent = 1.0f,
            
            gravity = 1f,
            
            fearStrength = 5.0f,
            fearExponent = 2.0f,
            
            attackDistRange = 2f,
            attackAngleRange = Mathf.PI,
            
            approachMovementStrength = 20.1f,
            approachMovementExponent = 0.5f,
            
            aggressionStrength = 4.4f,
            aggressionFalloff = 2.0f,
            aggressionDistanceCap = 10.0f,
            maxAggressionMultiplier = 1.6f,

            avoidCollisionWeight = 1000f,

            searchStrength = 10.4f,

            avoidanceStrength = 30.0f,
            
            randomMovements = 3.0f,

            hoverKi = 2f,
            hoverKp = 10f,
            targetHeight = 2f,

            fearMultiplier = 100f,

            colliderRadius = GetComponent<SphereCollider>().radius
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
