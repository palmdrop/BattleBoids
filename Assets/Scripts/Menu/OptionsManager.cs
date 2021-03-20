using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    void Start() {
        ApplySavedOptions();
    }

    private void ApplySavedOptions() {
        // Audio
        masterVolumeSlider.value = AudioManager.instance.GetMasterVolume();
        musicVolumeSlider.value = AudioManager.instance.GetMusicVolume();
        sfxVolumeSlider.value = AudioManager.instance.GetSoundEffectsVolume();

        // TODO Video
    }

    public void SaveOptions() {
        // Audio
        PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        PlayerPrefs.SetFloat("SfxVolume", sfxVolumeSlider.value);

        // TODO Video
    }

    public void SetMasterVolume() {
        AudioManager.instance.SetMasterVolume(masterVolumeSlider.value);
    }

    public void SetMusicVolume() {
        AudioManager.instance.SetMusicVolume(musicVolumeSlider.value);
    }

    public void SetSfxVolume() {
        AudioManager.instance.SetSoundEffectsVolume(sfxVolumeSlider.value);
    }
}
