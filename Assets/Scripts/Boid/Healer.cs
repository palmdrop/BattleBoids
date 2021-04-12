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
        health = maxHealth = 50;
        damage = 0;
        maxSpeed = 7f;
        avoidCollisionWeight = 5f;
        emotionalState = 0f;
        morale = moraleDefault = 1f;
        abilityDistance = 2.0f;

        timeBetweenActions = 0.2f;
        _healAmount = 1;
        _healRadius = 1.0f;

        ClassInfos.infos[(int)type] = new ClassInfo {
            type = this.type,
            viewRadius = 1.3f,
            separationRadius = 0.41f,
            fearRadius = 1.0f,
            maxForce = 7.0f,
            
            accelerationDesire = 0.1f,

            maxHealth = this.maxHealth,
            collisionAvoidanceDistance = 3f,
            collisionMask = (uint)this.collisionMask.value,
            groundMask = (uint)this.groundMask.value,
            abilityDistance = this.abilityDistance,

            alignmentStrength = 4.0f,
            alignmentExponent = 0.0f, 
            
            cohesionStrength = 3.2f,
            cohesionExponent = 0.0f,
            
            separationStrength = 220.0f,
            separationExponent = 1.5f,

            gravity = 1f,
            
            fearStrength = 12.0f,
            fearExponent = 0.8f,
            
            attackDistRange = 0f,
            attackAngleRange = Mathf.PI / 4.0f,
            
            approachMovementStrength = 10.0f,
            approachMovementExponent = 0.5f,
            
            aggressionStrength = 3.0f,
            aggressionFalloff = 1.5f,
            aggressionDistanceCap = 10.0f,
            maxAggressionMultiplier = 2.0f,

            avoidanceStrength = 40f,

            searchStrength = 5.4f,

            avoidCollisionWeight = 1000f,
            
            randomMovements = 1.5f,

            hoverKi = 2f,
            hoverKp = 10f,
            targetHeight = 2f
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
        if (!HasFriendlyTarget()
            || friendlyTarget.IsDead()
            || friendlyTarget.GetHealth() == friendlyTarget.GetMaxHealth()
            || math.distancesq(this.GetPos(), friendlyTarget.GetPos()) > _healRadius * _healRadius
            )
        {
            healBeam.SetActive(false);
            return;
        }
        
        friendlyTarget.ReceiveHealth(_healAmount);
        AnimateHeal(this.GetPos(), friendlyTarget.GetPos());
        healBeam.SetActive(true);
        if (Time.time - _previousAudioTime >= audioCooldown)
        {
            AudioManager.instance.PlaySoundEffectAtPoint(healingAudio, GetPos(), healingAudioVolume);
            _previousAudioTime = Time.time;
        }
    }

    private void AnimateHeal(Vector3 fromPos, Vector3 toPos) {
        _healBeamRenderer.startColor = new Color(0.5f, 0.5f, 0.0f, 0.5f);
        Vector3[] positions = new Vector3[] {fromPos, toPos};
        _healBeamRenderer.SetPositions(positions);
    }
}
