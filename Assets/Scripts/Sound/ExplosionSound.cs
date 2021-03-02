using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A class for playing explosion sounds
public class ExplosionSound : MonoBehaviour
{
    void Awake()
    {
        // Plays one of three variations of an explosion sound randomly
        float random = Random.Range(0f, 1f);
        if (random < 0.33f) FindObjectOfType<AudioManager>().Play("Explosion1");
        else if (random < 0.67f) FindObjectOfType<AudioManager>().Play("Explosion2");
        else FindObjectOfType<AudioManager>().Play("Explosion3");
    }
}
