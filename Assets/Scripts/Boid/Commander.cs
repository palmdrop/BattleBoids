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
        Cost = 50;
        health = maxHealth = 100;
        damage = 1;
        maxSpeed = 4f;
        avoidCollisionWeight = 5f;
        timeBetweenActions = 0.01f;
        emotionalState = 0f;
        morale = moraleDefault = 1f;
        abilityDistance = 0;
        meshDefaultLayer = LayerMask.NameToLayer("OutlineWhite");

        ClassInfos.infos[(int)type] = new ClassInfo {
            type = this.type,
            viewRadius = 1f,
            separationRadius = 0.32f,
            fearRadius = 0.5f,
            maxForce = 7.0f,
            
            accelerationDesire = 0.1f,

            maxHealth = this.maxHealth,
            collisionAvoidanceDistance = 3f,
            collisionMask = (uint)this.collisionMask.value,
            groundMask = (uint)this.groundMask.value,
            abilityDistance = this.abilityDistance,

            alignmentStrength = 2.5f,
            alignmentExponent = 0.0f,

            cohesionStrength = 3.0f,
            cohesionExponent = 0.0f,

            separationStrength = 220.0f,
            separationExponent = 1.0f,
            
            gravity = 100f,

            fearStrength = 5.0f,
            fearExponent = 1.5f,

            attackDistRange = 1f,
            attackAngleRange = Mathf.PI / 4.0f,

            approachMovementStrength = 0.5f,
            approachMovementExponent = 0.5f,

            aggressionStrength = 3.4f,
            aggressionFalloff = 0.3f,
            aggressionDistanceCap = 10.0f,
            maxAggressionMultiplier = 2.2f,

            searchStrength = 6.4f,

            avoidanceStrength = 30f,

            avoidCollisionWeight = 1000f,

            randomMovements = 0.4f,

            hoverKi = 2f,
            hoverKp = 10f,
            targetHeight = 2f,

            colliderRadius = GetComponent<SphereCollider>().radius
        };
    }

    void Update()
    {
        if (IsSelected() && Input.GetMouseButtonDown(2)) {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Ground"))) {
                var waypoint = Instantiate(waypointPrefab, hit.point, Quaternion.identity);
                foreach (var rend in waypoint.GetComponentsInChildren<MeshRenderer>()) {
                    rend.material.color = owner.color;
                }
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
            base.UpdateBoid(force + pathVec.normalized * 5f);
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

    public override void SetHidden(bool hidden)
    {
        base.SetHidden(hidden);
        foreach (GameObject waypoint in _path) {
            waypoint.SetActive(!hidden);
        }
    }

    protected override bool Act() {
        return true;
    }
}
