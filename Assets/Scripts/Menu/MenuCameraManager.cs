using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuCameraManager : MonoBehaviour
{
    [SerializeField] private Camera[] cameras;

    private Camera _main;
    private Camera _campaign;
    private Camera _multiplayer;
    private Camera _settings;
    private Camera _credits;

    // Start is called before the first frame update
    void Start() {
        _main = cameras[0];
        _campaign = cameras[1];
        _multiplayer = cameras[2];
        _settings = cameras[3];
        _credits = cameras[4];
    }

    public void Main() {
        Show(_main);
    }

    public void Campaign() {
        Show(_campaign);
    }

    public void Multiplayer() {
        Show(_multiplayer);
    }

    public void Settings() {
        Show(_settings);
    }

    public void Credits() {
        Show(_credits);
    }

    private void Show(Camera show) {
        foreach (Camera camera in cameras) {
            if (camera != show) {
                camera.enabled = false;
            } else {
                camera.enabled = true;
            }
        }
    }
}
