using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [SerializeField] private FullscreenButtons fullscreen;
    [SerializeField] private Dropdown resolutionDropdown;

    private Resolution[] _resolutions;
    private int _defaultResolutionIndex;

    public void Start() {
        _resolutions = Screen.resolutions;
        _defaultResolutionIndex = _resolutions.Length - 1;
        ApplySavedOptions();
        InitResolutionDropdown();
    }

    private void ApplySavedOptions() {
        // Audio
        masterVolumeSlider.value = AudioManager.instance.GetMasterVolume();
        musicVolumeSlider.value = AudioManager.instance.GetMusicVolume();
        sfxVolumeSlider.value = AudioManager.instance.GetSoundEffectsVolume();

        // Video
        fullscreen.SetButtons(Boolean.Parse(PlayerPrefs.GetString("Fullscreen", "true")));
        Resolution defaultResolution = _resolutions[_defaultResolutionIndex];
        int resolutionWidth = PlayerPrefs.GetInt("ResolutionWidth", defaultResolution.width);
        int resolutionHeigth = PlayerPrefs.GetInt("ResolutionHeight", defaultResolution.height);
        int refreshRate = PlayerPrefs.GetInt("RefreshRate", defaultResolution.refreshRate);
        Screen.SetResolution(resolutionWidth, resolutionHeigth, fullscreen.IsOn(), refreshRate);
    }

    private void InitResolutionDropdown() {
        resolutionDropdown.ClearOptions();
        Dropdown.OptionData option;
        foreach (Resolution resolution in _resolutions) {
            option = new Dropdown.OptionData();
            option.text = resolution.ToString();
            resolutionDropdown.options.Add(option);
        }
        int index = PlayerPrefs.GetInt("ResolutionIndex", _defaultResolutionIndex);
        resolutionDropdown.value = index < _defaultResolutionIndex ? index : _defaultResolutionIndex;
        resolutionDropdown.captionText.text = resolutionDropdown.options[resolutionDropdown.value].text;
    }

    public void SaveOptions() {
        // Audio
        PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        PlayerPrefs.SetFloat("SfxVolume", sfxVolumeSlider.value);

        // Video
        PlayerPrefs.SetString("Fullscreen", fullscreen.IsOn().ToString());
        Resolution resolution = _resolutions[resolutionDropdown.value];
        PlayerPrefs.SetInt("ResolutionWidth", resolution.width);
        PlayerPrefs.SetInt("ResolutionHeight", resolution.height);
        PlayerPrefs.SetInt("RefreshRate", resolution.refreshRate);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);
    }

    public void UpdateResolution() {
        if (resolutionDropdown != null) {
            Resolution resolution = _resolutions[resolutionDropdown.value];
            Screen.SetResolution(resolution.width, resolution.height, fullscreen.IsOn(), resolution.refreshRate);
        }
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
