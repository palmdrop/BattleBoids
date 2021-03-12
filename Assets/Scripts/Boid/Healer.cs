using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class Healer : Boid
{
    private int _healAmount;
    private float _healRadius;
    
    [SerializeField] private GameObject healBeam;
    [SerializeField] private AudioClip healingAudio;
    [Range(0f, 1f)] public float healingAudioVolume;
    public float audioCooldown;
    private float _previousAudioTime = 0f;
    private LineRenderer _healBeamRenderer;
    
    // Start is called before the first frame update
    void Start() {
        base.Start();

        Cost = 20;
        type = Type.Healer;
        health = maxHealth = 100;
        damage = 0;
        maxSpeed = 4f;
        targetHeight = 2f;
        collisionAvoidanceDistance = 3f;
        avoidCollisionWeight = 5f;
        hoverKi = 2f;
        hoverKp = 10f;
        emotionalState = 0f;
        morale = moraleDefault = 1f;
        abilityDistance = 3.0f;
        
        timeBetweenActions = 0.03f;
        _healAmount = 1;
        _healRadius = 1.0f;
        
        classInfo = new ClassInfo {
            viewRadius = 3f,
            separationRadius = 0.3f,
            fearRadius = 1.0f,
            maxForce = 2.0f,
            
            alignmentStrength = 6.0f,
            alignmentExponent = 0.0f, 
            
            cohesionStrength = 6.0f,
            cohesionExponent = 0.0f,
            
            separationStrength = 120.0f,
            separationExponent = 1.0f,

            gravity = 1f,
            
            fearStrength = 250.0f,
            fearExponent = 1.4f,
            
            attackDistRange = 0f,
            attackAngleRange = Mathf.PI / 4.0f,
            
            approachMovementStrength = 35.0f,
            approachMovementExponent = 0.5f,
            
            aggressionStrength = 7.4f,
            
            randomMovements = 4.0f,
        };
        
        _healBeamRenderer = healBeam.GetComponent<LineRenderer>();
    }

    protected override void Act()
    {
        // Healers cannot attack
        Heal();
    }

    private void Heal()
    {
        // Do not heal if there is no target or if the target has max health
        // Do not heal if target is out of range
        if (!HasTarget()
            || target.GetHealth() == target.GetMaxHealth()
            || math.distancesq(this.GetPos(), target.GetPos()) > _healRadius * _healRadius
            )
        {
            healBeam.SetActive(false);
            return;
        }
        
        target.ReceiveHealth(_healAmount);
        AnimateHeal(this.GetPos(), target.GetPos());
        healBeam.SetActive(true);
        if (Time.time - _previousAudioTime >= audioCooldown)
        {
            FindObjectOfType<AudioManager>().PlayAtPoint(healingAudio, GetPos(), healingAudioVolume);
            _previousAudioTime = Time.time;
        }
    }

    private void AnimateHeal(Vector3 fromPos, Vector3 toPos) {
        _healBeamRenderer.startColor = new Color(0.5f, 0.5f, 0.0f, 0.5f);
        Vector3[] positions = new Vector3[] {fromPos, toPos};
        _healBeamRenderer.SetPositions(positions);
    }
}
