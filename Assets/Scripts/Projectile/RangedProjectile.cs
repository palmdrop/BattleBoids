using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedProjectile : MonoBehaviour
{
    [SerializeField] private GameObject hitAnimation;
    [SerializeField] protected Material baseMaterial;

    private Player _owner;
    private int _damage;
    private GameObject _hitAnimation;
    private Vector3 vel;
    private Vector3 gravity = Physics.gravity;
    public static float projectileRadius = 0.05f;
    private static List<Material> materials = new List<Material>();

    void Update() {
        // If under map destroy
        //if (transform.position.y < 0f) {
        //    gameObject.SetActive(false);
            //Destroy(gameObject);
        //}
    }

    public void SetForce(Vector3 force)
    {
        vel = force;
    }
    public Vector3 GetVel()
    {
        return vel;
    }


    private void fastPhysicsUpdate(float dt)
    {
        vel += gravity * dt;
        transform.position += vel * dt;
    }

    public void ManagedFixedUpdate()
    {
        fastPhysicsUpdate(Time.fixedDeltaTime);
    }

    public void ManagedOnTriggerEnter(Boid boid) {

        if (boid != null) { // Collision with boid
            if (boid.GetOwner() != _owner) { // Enemy
                boid.TakeDamage(_damage);

                /*_hitAnimation = Instantiate(
                    hitAnimation,
                    gameObject.transform.position,
                    gameObject.transform.rotation
                );
                _hitAnimation.GetComponent<ParticleSystem>().startColor = _owner.color;
                Destroy(_hitAnimation,
                    _hitAnimation.GetComponent<ParticleSystem>().main.duration
                );*/
                GameObject hit = ParticlePoolManager.SharedInstance.getPooledObject(ParticlePoolManager.Type.Hit);
                if (hit != null)
                {
                    hit.transform.position = transform.position;
                    hit.transform.rotation = transform.rotation;
                    hit.SetActive(true);
                    hit.GetComponent<ParticleSystem>().startColor = _owner.color;
                }

                //Destroy(gameObject);
                gameObject.SetActive(false);
            } else { // Friendly
                return;
            }
        } else { // Collision with environment
            //Destroy(gameObject);
            gameObject.SetActive(false);
        }
    }

    //private void SetColor() {
    /*foreach (GameObject particleObject in particleObjects) {
        particleObject.GetComponent<ParticleSystem>().startColor = _owner.color;
    }
    TrailRenderer tr = gameObject.GetComponent<TrailRenderer>();
    Color trColor = new Color(_owner.color.r, _owner.color.g, _owner.color.b, 0.1f);
    tr.startColor = trColor;
    tr.endColor = trColor;*/
    //}

    public void SetColor(Color color)
    {
        TrailRenderer tr = gameObject.GetComponent<TrailRenderer>();
        Color trColor = new Color(color.r, color.g, color.b, 0.1f);
        tr.startColor = trColor;
        tr.endColor = trColor;

        foreach (Material material in materials)
        {
            if (color.Equals(material.color))
            {
                transform.GetChild(0).GetComponent<MeshRenderer>().material = material;
                return;
            }
        }
        Material tmp = new Material(baseMaterial);
        tmp.color = color;
        materials.Add(tmp);
        transform.GetChild(0).GetComponent<MeshRenderer>().material = tmp;
    }

    public void SetColor()
    {
        SetColor(_owner.color);
    }

    public void SetOwner(Player owner) {
        _owner = owner;
    }

    public void SetDamage(int damage) {
        _damage = damage;
    }
}
