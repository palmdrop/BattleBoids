using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Audio;

// A class for managing playing sounds
public class AudioManager : MonoBehaviour
{
    // The sounds that can be played from this manager
    public Sound[] musicTracks;
    private float _masterVolume = 1f;
    private float _savedMasterVolume = 1f;
    private float _effectsVolume = 1f;
    private float _musicVolume = 1f;
    private bool _isMuted = false;

    
    void Awake()
    {
        foreach (Sound s in musicTracks)
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
    public void PlayMusic(string name)
    {
        Sound s = Array.Find(musicTracks, sound => sound.name == name);
        s.source.Play();
    }


    public void StopMusic(string name)
    {
        Sound s = Array.Find(musicTracks, sound => sound.name == name);
        s.source.Stop();
    }


    public void PlaySoundEffectAtPoint(AudioClip clip, Vector3 point, float volume)
    {
        AudioSource.PlayClipAtPoint(clip, point, _masterVolume * _effectsVolume * volume);
    }


    public void SetMasterVolume(float volume)
    {
        // Make sure the volume is between 0 and 1
        _masterVolume = Math.Min(Math.Max(0f, volume), 1f);
        
        // Update the sounds (music) that are playing
        foreach (Sound s in musicTracks)
        {
            s.source.volume = s.volume * _masterVolume * _musicVolume;
        }

        _isMuted = false;
    }

    public float GetMasterVolume()
    {
        return _masterVolume;
    }

    public void SetSoundEffectsVolume(float volume)
    {
        _effectsVolume = Math.Min(Math.Max(0f, volume), 1f);
    }

    public float GetSoundEffectsVolume()
    {
        return _effectsVolume;
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = Math.Min(Math.Max(0f, volume), 1f);
        foreach (Sound s in musicTracks)
        {
            s.source.volume = s.volume * _masterVolume * _musicVolume;
        }
    }

    public float GetMusicVolume()
    {
        return _musicVolume;
    }

    // Toggles music and sounds to mute
    public void ToggleMute()
    {
        bool tempIsMuted = _isMuted;

        if (!_isMuted)
        {
            // If we just muted, save the current volume to be able to toggle back to it
            _savedMasterVolume = _masterVolume;
            SetMasterVolume(0f);
        } else
        {
            // Toggle back to the saved volume
            SetMasterVolume(_savedMasterVolume);
        }

        _isMuted = !tempIsMuted;
    }
}
