using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Text boins;

    // Start is called before the first frame update
    void Start()
    {
        UpdateBoins();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateBoins()
    {
        boins.text = player.GetBoins().ToString();
    }
}
