using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionSound : MonoBehaviour
{

    //public Sound[] variations;

    void Awake()
    {
        float random = Random.Range(0f, 1f);
        if (random < 0.33f) FindObjectOfType<AudioManager>().Play("Explosion1");
        else if (random < 0.67f) FindObjectOfType<AudioManager>().Play("Explosion2");
        else FindObjectOfType<AudioManager>().Play("Explosion3");

        //for (int i = 0; i < variations.Length; i++)
        //{
        //    if (random < ((float) i) / variations.Length)
        //    {
        //        AudioSource source = gameObject.AddComponent<AudioSource>();
        //        source.clip = variations[i].clip;
        //        source.volume = variations[i].volume;
        //        source.pitch = variations[i].pitch;
        //        source.loop = variations[i].loop;
        //        source.spatialBlend = 0.75f;
        //        source.Play();
        //        break;
        //    }
        //}



    }
}
