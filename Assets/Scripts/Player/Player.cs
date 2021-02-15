using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private List<GameObject> flock = new List<GameObject>();
    [SerializeField] private int boins; // Currency to buy boids
    [SerializeField] private int score; // Points obtained during a match
    [SerializeField] public int id; // Identifier
    [SerializeField] public Color color; // Player color
    [SerializeField] private string nickname; // Unique

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public List<GameObject> GetFlock()
    {
        return flock;
    }

    public void AddBoins(int boinsToAdd)
    {
        boins += boinsToAdd;
    }

    public bool RemoveBoins(int boinsToRemove)
    {
        bool isSuccess;
        if (isSuccess = boinsToRemove <= boins)
        {
            boins -= boinsToRemove;
        }
        return isSuccess;
    }

    public int GetBoins()
    {
        return boins;
    }

    public void AddScore(int pointsToAdd)
    {
        score += pointsToAdd;
    }

    public string GetNickname()
    {
        return nickname;
    }
}
