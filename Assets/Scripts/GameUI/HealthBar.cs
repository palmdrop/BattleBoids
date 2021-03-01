using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fill;
    private Image[] _images;
    private Boid _boid;
    private Renderer _renderer;

    // Start is called before the first frame update
    void Start()
    {
        _images = GetComponentsInChildren<Image>();
        _renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        fill.fillAmount = _boid.GetHealth() / _boid.GetMaxHealth();
        transform.position = _boid.GetPos() + new Vector3(0, 1, 0);
        transform.forward = Camera.main.transform.forward;
        if (_boid.IsDead()) {
            Destroy(gameObject);
        }
        foreach (Image img in _images) {
            var col = img.color;
            if (Camera.main.transform.position.y > 10f ||
                !_boid.GetOwner().GetGameUI().ShowHealthBars()) {
                col.a = 0;
            } else {
                col.a = 1;
            }
            img.color = col;
        }
    }

    public void SetOwner(Boid b)
    {
        this._boid = b;
    }
}
