using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Commander : Boid
{
    [SerializeField] private GameObject waypointPrefab;

    private List<GameObject> _path = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        base.Start();

        type = Type.Commander;
        Cost = 10;
        health = maxHealth = 100;
        damage = 1;
        maxSpeed = 4f;
        avoidCollisionWeight = 5f;
        timeBetweenActions = 0.01f;
        emotionalState = 0f;
        morale = moraleDefault = 1f;
        abilityDistance = 0;

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

            confidenceThreshold = 0.5f,

            alignmentStrength = 5.6f,
            alignmentExponent = 0.0f,

            cohesionStrength = 6.0f,
            cohesionExponent = 0.0f,

            separationStrength = 120.0f,
            separationExponent = 1.0f,
            
            gravity = 40f,

            fearStrength = 60.0f,
            fearExponent = 1.5f,

            attackDistRange = 1f,
            attackAngleRange = Mathf.PI / 4.0f,

            approachMovementStrength = 20.1f,
            approachMovementExponent = 0.5f,

            aggressionStrength = 7.4f,
            aggressionFalloff = 2.0f,
            aggressionDistanceCap = 10.0f,
            maxAggressionMultiplier = 2.0f,
            
            searchStrength = 10.4f,
            
            avoidanceStrength = 70f,

            avoidCollisionWeight = 100f,

            randomMovements = 6.0f,

            hoverKi = 2f,
            hoverKp = 10f,
            targetHeight = 2f
        };
    }

    void Update()
    {
        if (IsSelected() && Input.GetMouseButtonDown(2)) {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Ground"))) {
                var waypoint = Instantiate(waypointPrefab, hit.point, Quaternion.identity);
                _path.Add(waypoint);
            }
        }
    }

    public override void UpdateBoid(Vector3 force)
    {
        if (_path.Count == 0) {
            base.UpdateBoid(force);
        } else {
            var pathVec = _path[0].transform.position - GetPos();
            base.UpdateBoid(force + pathVec.normalized * 3f);
            if (pathVec.magnitude < 3f) {
                _path.RemoveAt(0);
            }
        }
    }

    void OnDestroy()
    {
        foreach (var waypoint in _path) {
            Destroy(waypoint);
        }
    }

    protected override void Act() {
    }
}
