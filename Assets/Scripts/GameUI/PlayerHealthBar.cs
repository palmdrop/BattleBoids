using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private GameObject fill;
    [SerializeField] private Player player;
    [SerializeField] private Text readyText;
    [SerializeField] private GameObject selected;
    private Image _image;
    private GameManager _gameManager;

    private Color ready = new Color(0.08f, 0.949f, 0.216f);
    private Color unReady = new Color(0.333f, 0.333f, 0.333f);

    // Start is called before the first frame update
    void Start()
    {
        _image = fill.GetComponent<Image>();
        _gameManager = GetComponentInParent<GameManager>();
        var text = GetComponentInChildren<Text>();
        text.text = player.GetNickname();
        text.color = player.color;
    }

    // Update is called once per frame
    void Update()
    {
        float sumHealth = 0;
        float sumMaxHealth = 0;
        foreach (Boid boid in player.GetFlock()) {
            sumHealth += boid.GetHealth();
            sumMaxHealth += boid.GetMaxHealth();
        }
        _image.fillAmount = sumHealth / sumMaxHealth;
        UpdateReady();
        UpdateActivePlayer();
    }

    private void UpdateReady() {
        readyText.color = player.IsReady() ? ready : unReady;
    }

    private void UpdateActivePlayer() {
        selected.SetActive(player.IsActive() && _gameManager.GetState() == GameManager.GameState.Placement);
    }
}
