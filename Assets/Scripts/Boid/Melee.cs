using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class Melee : Boid {

    [SerializeField] private GameObject laser;

    private float _nextAttackTime;

    // Start is called before the first frame update
    void Start() {
        base.Start();
        
        dead = false;
        collisionMask = LayerMask.GetMask("Wall", "Obstacle");
        type = Type.Melee;
        cost = 10;
        health = maxHealth = 100;
        damage = 1;
        maxSpeed = 4f;
        targetHeight = 2f;
        collisionAvoidanceDistance = 3f;
        avoidCollisionWeight = 5f;
        hoverKi = 2f;
        hoverKp = 10f;
        timeBetweenAttacks = 0.01f;
        emotionalState = 0f;
        morale = moraleDefault = 1f;
        abilityDistance = 0;

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
            
            attackDistRange = 1f,
            attackAngleRange = Mathf.PI / 4.0f,
            
            attackMovementStrength = 20.1f,
            attackMovementExponent = 0.5f,
            
            aggressionStrength = 10.4f,
            
            randomMovements = 6.0f,
        };

        laser.SetActive(false);
    }

    public override void Attack() {
        if (target != null && Time.time > _nextAttackTime) {
            _nextAttackTime = Time.time + timeBetweenAttacks;
            target.TakeDamage(damage);
            PlayAttackSound();
            SetLaser(this.GetPos(), target.GetPos());
            laser.SetActive(true);
        } else {
            laser.SetActive(false);
        }
    }

    private void SetLaser(Vector3 fromPos, Vector3 toPos) {
        LineRenderer lineRenderer = laser.GetComponent<LineRenderer>();
        lineRenderer.startColor = owner.color;
        Vector3[] positions = new Vector3[] {fromPos, toPos};
        lineRenderer.SetPositions(positions);
    }

    // Plays an attack sound
    private void PlayAttackSound()
    {
        // If there are too many sounds, this line can be uncommented
        //if (Random.Range(0f, 1f) < 0.9) return;

        // Plays one of three variations of an attack sound randomly
        float random = Random.Range(0f, 1f);
        if (random < 0.33f) FindObjectOfType<AudioManager>().Play("Laser1");
        else if (random < 0.67f) FindObjectOfType<AudioManager>().Play("Laser2");
        else FindObjectOfType<AudioManager>().Play("Laser3");
    }
}
