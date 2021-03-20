using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayOnce : MonoBehaviour
{

    [SerializeField] private AudioClip clip;
    [Range(0f, 1f)] public float volume;

    // Start is called before the first frame update
    void Start()
    {
        AudioManager.instance.PlaySoundEffectAtPoint(clip, GetComponentInParent<Transform>().position, volume);
    }
}
