using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] List<GameObject> menus;
    [SerializeField] GameObject background;

    private GameObject _mainMenu;
    private GameObject _campaignMenu;
    private GameObject _multiplayerMenu;
    private GameObject _optionsMenu;
    private GameObject _creditsMenu;
    private GameObject _loadingScreen;

    void Start() {
        _mainMenu = menus[0];
        _campaignMenu = menus[1];
        _multiplayerMenu = menus[2];
        _optionsMenu = menus[3];
        _creditsMenu = menus[4];
        _loadingScreen = menus[5];

        FindObjectOfType<AudioManager>().Play("MenuMusic");
    }

    public void MainMenu() {
        Show(_mainMenu);
    }

    public void CampaignMenu() {
        Show(_campaignMenu);
    }

    public void MultiplayerMenu() {
        Show(_multiplayerMenu);
    }

    public void OptionsMenu() {
        Show(_optionsMenu);
    }

    public void CreditsMenu() {
        Show(_creditsMenu);
    }

    public void Quit() {
        Application.Quit();
    }

    public void LoadingScreen() {
        Show(_loadingScreen);
        // TODO change background
    }

    public IEnumerator LoadSceneAsync(string scene) {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone) {
            yield return null;
        }
    }

    private void Show(GameObject show) {
        foreach (GameObject menu in menus) {
            if (menu != show) {
                menu.SetActive(false);
            } else {
                menu.SetActive(true);
            }
        }
    }
}
