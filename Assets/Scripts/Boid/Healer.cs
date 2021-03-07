using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class Healer : Boid
{
    private int _healAmount;
    
    [SerializeField] private GameObject healBeam;
    
    // Start is called before the first frame update
    void Start() {
        base.Start();

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
        
        timeBetweenActions = 0.03f;
        _healAmount = 1;
        
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

            gravity = 1f,
            
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

    public override void Act()
    {
        // Healers cannot attack
        Heal();
    }

    private void Heal()
    {
        // Do not heal if there is no target or if the target has max health
        // Do not heal if target is out of range
        if (target == null 
            || target.GetHealth() == target.GetMaxHealth()
            || math.distancesq(this.GetPos(), target.GetPos()) > classInfo.healRadius * classInfo.healRadius
            )
        {
            healBeam.SetActive(false);
            return;
        }
        
        target.ReceiveHealth(_healAmount);
        AnimateHeal(this.GetPos(), target.GetPos());
        healBeam.SetActive(true);
    }

    private void AnimateHeal(Vector3 fromPos, Vector3 toPos) {
        LineRenderer lineRenderer = healBeam.GetComponent<LineRenderer>();
        lineRenderer.startColor = new Color(0.5f, 0.5f, 0.0f, 0.5f);
        Vector3[] positions = new Vector3[] {fromPos, toPos};
        lineRenderer.SetPositions(positions);
    }
}
