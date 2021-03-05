using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private GameObject fill;
    [SerializeField] private Player player;
    private Image _image;

    // Start is called before the first frame update
    void Start()
    {
        _image = fill.GetComponent<Image>();
        var text = GetComponentInChildren<Text>();
        text.text = player.GetNickname();
        text.color = player.color;
    }

    // Update is called once per frame
    void Update()
    {
        float sumHealth = 0;
        float sumMaxHealth = 0;
        foreach (GameObject o in player.GetFlock()) {
            var boid = o.GetComponent<Boid>();
            sumHealth += boid.GetHealth();
            sumMaxHealth += boid.GetMaxHealth();
        }
        _image.fillAmount = sumHealth / sumMaxHealth;
    }
}
