using UnityEngine;

public class Melee : Boid {

    [SerializeField] private GameObject laser;
    private LineRenderer _laserRenderer;

    // Start is called before the first frame update
    void Start() {
        base.Start();
        
        type = Type.Melee;
        Cost = 10;
        health = maxHealth = 100;
        damage = 1;
        maxSpeed = 4f;
        targetHeight = 2f;
        collisionAvoidanceDistance = 3f;
        avoidCollisionWeight = 5f;
        hoverKi = 2f;
        hoverKp = 10f;
        timeBetweenActions = 0.01f;
        emotionalState = 0f;
        morale = moraleDefault = 1f;
        abilityDistance = 0;

        classInfo = new ClassInfo {
            viewRadius = 3f,
            separationRadius = 0.3f,
            fearRadius = 1.0f,
            maxForce = 2f,
            
            confidenceThreshold = 1.0f,
            
            alignmentStrength = 5.6f,
            alignmentExponent = 0.0f, 
            
            cohesionStrength = 4.0f,
            cohesionExponent = 0.0f,
            
            separationStrength = 120.0f,
            separationExponent = 1.0f,

            gravity = 1f,
            
            fearStrength = 140.0f,
            fearExponent = 1.0f,
            
            attackDistRange = 1f,
            attackAngleRange = Mathf.PI / 4.0f,
            
            approachMovementStrength = 20.1f,
            approachMovementExponent = 0.5f,
            
            aggressionStrength = 10.4f,
            
            randomMovements = 6.0f,
        };

        _laserRenderer = laser.GetComponent<LineRenderer>();
        laser.SetActive(false);
    }

    protected override void Act()
    {
        Attack();
    }

    private void Attack() 
    {
        if (HasTarget()) {
            target.TakeDamage(damage);
            SetLaser(this.GetPos(), target.GetPos());
            laser.SetActive(true);
        } else {
            laser.SetActive(false);
        }
    }

    private void SetLaser(Vector3 fromPos, Vector3 toPos) {
        _laserRenderer.startColor = owner.color;
        Vector3[] positions = new Vector3[] {fromPos, toPos};
        _laserRenderer.SetPositions(positions);
    }
}
