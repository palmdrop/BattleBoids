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
            
            confidenceThreshold = 0.5f,

            alignmentStrength = 5.6f,
            alignmentExponent = 0.0f,

            cohesionStrength = 4.0f,
            cohesionExponent = 0.0f,

            separationStrength = 120.0f,
            separationExponent = 1.0f,
            
            gravity = 40f,

            fearStrength = 140.0f,
            fearExponent = 1.0f,

            attackDistRange = 1f,
            attackAngleRange = Mathf.PI / 4.0f,

            approachMovementStrength = 20.1f,
            approachMovementExponent = 0.5f,

            aggressionStrength = 10.4f,

            randomMovements = 6.0f,
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
