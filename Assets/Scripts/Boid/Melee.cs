using UnityEngine;

public class Melee : Boid {

    [SerializeField] private GameObject laser;

    // Start is called before the first frame update
    void Start() {
        base.Start();
        
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
        timeBetweenActions = 0.01f;
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
            
            approachMovementStrength = 20.1f,
            approachMovementExponent = 0.5f,
            
            aggressionStrength = 10.4f,
            
            randomMovements = 6.0f,
        };

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
        LineRenderer lineRenderer = laser.GetComponent<LineRenderer>();
        lineRenderer.startColor = owner.color;
        Vector3[] positions = new Vector3[] {fromPos, toPos};
        lineRenderer.SetPositions(positions);
    }
}
