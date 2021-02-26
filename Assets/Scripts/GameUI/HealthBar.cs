using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private GameObject fill;
    private Image _image;
    private Boid _boid;

    // Start is called before the first frame update
    void Start()
    {
        _image = fill.GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        _image.fillAmount = _boid.GetHealth() / _boid.GetMaxHealth();
        transform.position = _boid.GetPos() + new Vector3(0, 1, 0);
        transform.forward = Camera.main.transform.forward;
        if (_boid.IsDead()) {
            Destroy(gameObject);
        }
    }

    public void SetOwner(Boid b)
    {
        this._boid = b;
    }
}
