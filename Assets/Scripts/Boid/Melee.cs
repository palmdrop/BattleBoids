using UnityEngine;

public class Melee : Boid {

    [SerializeField] private GameObject laser;
    [SerializeField] private AudioClip laserAudio;
    [Range(0f, 1f)] public float laserAudioVolume;
    public float audioCooldown;
    private float _previousAudioTime = 0f;
    private LineRenderer _laserRenderer;

    // Start is called before the first frame update
    void Start() {
        base.Start();
        
        type = Type.Melee;
        Cost = 10;
        health = maxHealth = 80;
        damage = 1;
        boostedDamage = 5;
        maxSpeed = 4f;
        avoidCollisionWeight = 5f;
        timeBetweenActions = 0.05f;
        emotionalState = 0f;
        morale = moraleDefault = 1f;
        abilityDistance = 0;

        ClassInfos.infos[(int) type] = new ClassInfo {
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

            confidenceThreshold = 1.0f,
            
            alignmentStrength = 5.6f,
            alignmentExponent = 0.0f, 
            
            cohesionStrength = 5.0f,
            cohesionExponent = 0.0f,
            
            separationStrength = 120.0f,
            separationExponent = 1.0f,

            gravity = 1f,
            
            fearStrength = 50.0f,
            fearExponent = 1.8f,
            
            attackDistRange = 1f,
            attackAngleRange = Mathf.PI / 4.0f,
            
            approachMovementStrength = 30.1f,
            approachMovementExponent = 0.5f,
            
            aggressionStrength = 5.4f,

            avoidCollisionWeight = 100f,

            searchStrength = 10.4f,
            
            randomMovements = 6.0f,

            hoverKi = 2f,
            hoverKp = 10f,
            targetHeight = 2f
    };

        _laserRenderer = laser.GetComponent<LineRenderer>();
        laser.SetActive(false);
        SetLaserColor();
    }

    protected override void Act()
    {
        Attack();
    }

    private void Attack() 
    {
        if (HasTarget() && !target.IsDead()) {
            target.TakeDamage(IsBoosted() ? boostedDamage : damage);
            SetLaser(this.GetPos(), target.GetPos());
            laser.SetActive(true);

            if (Time.time - _previousAudioTime >= audioCooldown)
            {
                AudioManager.instance.PlaySoundEffectAtPoint(laserAudio, GetPos(), laserAudioVolume);
                _previousAudioTime = Time.time;
            }
        } else {
            laser.SetActive(false);
        }
    }

    private void SetLaserColor() {
        Color start = new Color(owner.color.r, owner.color.g, owner.color.b, 0.5f);
        Color end = new Color(owner.color.r, owner.color.g, owner.color.b, 0f);
        _laserRenderer.SetColors(start, end);
    }

    private void SetLaser(Vector3 fromPos, Vector3 toPos) {
        Vector3[] positions = new Vector3[] {fromPos, toPos};
        _laserRenderer.SetPositions(positions);
    }
}
