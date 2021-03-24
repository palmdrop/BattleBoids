using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBar_V2 : MonoBehaviour
{
    MaterialPropertyBlock _blockMaterial;
    MeshRenderer _renderer;
    Boid _boid;


    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _blockMaterial = new MaterialPropertyBlock();
        _boid = GetComponentInParent<Boid>();
    }


    void Update()
    {
        if (_boid.GetHealth() < _boid.GetMaxHealth())
        {
            _renderer.enabled = true;
            AlignCamera();
            UpdateHealth();
        }
        else
        {
            _renderer.enabled = false;
        }
        if (_boid.IsDead())
        {
            Destroy(gameObject);
        }
    }

    private void UpdateHealth()
    {
        _renderer.GetPropertyBlock(_blockMaterial);
        _blockMaterial.SetFloat("_Fill", _boid.GetHealth() / _boid.GetMaxHealth());
        _renderer.SetPropertyBlock(_blockMaterial);
    }

    private void AlignCamera()
    {
        Vector3 forward = (transform.position - Camera.main.transform.position).normalized;
        Vector3 up = Vector3.Cross(forward, Camera.main.transform.right);
        transform.rotation = Quaternion.LookRotation(forward, up);
    }
}
