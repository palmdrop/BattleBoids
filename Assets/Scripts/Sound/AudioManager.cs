using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Audio;

// A class for managing playing sounds
public class AudioManager : MonoBehaviour
{
    // The sounds that can be played from this manager
    public Sound[] sounds;
    private float _masterVolume = 1f;
    private float _savedVolume = 1f;
    private bool _isMuted = false;

    
    void Awake()
    {
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    // Plays a sound from a given name. Name must be in a Sound object of the sounds array
    // Call this method from an event when you want to play a sound
    public void Play(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Play();
    }


    public void Stop(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        s.source.Stop();
    }


    public void PlayAtPoint(AudioClip clip, Vector3 point, float volume)
    {
        AudioSource.PlayClipAtPoint(clip, point, _masterVolume * volume);
    }


    public void SetMasterVolume(float volume)
    {
        // Make sure the volume is between 0 and 1
        _masterVolume = Math.Min(Math.Max(0f, volume), 1f);
        
        // Update the sounds (music) that are playing
        foreach (Sound s in sounds)
        {
            s.source.volume = s.volume * _masterVolume;
        }

        _isMuted = false;
    }

    public float GetMasterVolume()
    {
        return _masterVolume;
    }

    // Toggles music and sounds to mute
    public void ToggleMute()
    {
        bool tempIsMuted = _isMuted;

        if (!_isMuted)
        {
            // If we just muted, save the current volume to be able to toggle back to it
            _savedVolume = _masterVolume;
            SetMasterVolume(0f);
        } else
        {
            // Toggle back to the saved volume
            SetMasterVolume(_savedVolume);
        }

        _isMuted = !tempIsMuted;
    }
}
