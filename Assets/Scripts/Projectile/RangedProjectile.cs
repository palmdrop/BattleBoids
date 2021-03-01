using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedProjectile : MonoBehaviour
{
    [SerializeField] private GameObject hitAnimation;
    [SerializeField] private GameObject[] particleObjects;

    private Player _owner;
    private int _damage;
    private GameObject _hitAnimation;

    void Start() {
        SetColor();
    }

    void Update() {
        // If under map destroy
        if (transform.position.y < -10f) {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision) {
        GameObject hit = collision.gameObject;
        Boid boid = hit.GetComponent<Boid>();

        if (boid != null) { // Collision with boid
            if (boid.GetOwner() != _owner) { // Enemy
                hit.GetComponent<Boid>().TakeDamage(_damage);
                _hitAnimation = Instantiate(
                    hitAnimation,
                    gameObject.transform.position,
                    gameObject.transform.rotation
                );
                _hitAnimation.GetComponent<ParticleSystem>().startColor = _owner.color;
                Destroy(_hitAnimation,
                    _hitAnimation.GetComponent<ParticleSystem>().main.duration
                );
                Destroy(gameObject);
            } else { // Friendly
                return;
            }
        } else { // Collision with environment
            Destroy(gameObject);
        }
    }

    private void SetColor() {
        foreach (GameObject particleObject in particleObjects) {
            particleObject.GetComponent<ParticleSystem>().startColor = _owner.color;
        }
        TrailRenderer tr = gameObject.GetComponent<TrailRenderer>();
        tr.startColor = new Color(
            _owner.color.r,
            _owner.color.g,
            _owner.color.b,
            0.1f
        );
    }

    public void SetOwner(Player owner) {
        _owner = owner;
    }

    public void SetDamage(int damage) {
        _damage = damage;
    }
}
