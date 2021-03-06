using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> menus;
    [SerializeField] private MenuCameraManager cameras;
    [SerializeField] private GameObject loadingScreen;

    private GameObject _mainMenu;
    private GameObject _campaignMenu;
    private GameObject _multiplayerMenu;
    private GameObject _optionsMenu;
    private GameObject _creditsMenu;

    void Start() {
        _mainMenu = menus[0];
        _campaignMenu = menus[1];
        _multiplayerMenu = menus[2];
        _optionsMenu = menus[3];
        _creditsMenu = menus[4];

        AudioManager.instance.PlayMusic("MenuMusic");
        _optionsMenu.GetComponent<OptionsManager>().Start();
    }

    public void MainMenu() {
        Show(_mainMenu);
        cameras.Main();
    }

    public void CampaignMenu() {
        Show(_campaignMenu);
        cameras.Campaign();
    }

    public void MultiplayerMenu() {
        Show(_multiplayerMenu);
        cameras.Multiplayer();
    }

    public void OptionsMenu() {
        Show(_optionsMenu);
        cameras.Settings();
    }

    public void CreditsMenu() {
        Show(_creditsMenu);
        cameras.Credits();
    }

    public void Quit() {
        Application.Quit();
    }

    public void Play(SceneData.GameSettings settings) {
        LoadingScreen(settings);
        IEnumerator loadSceneAsync = LoadSceneAsync(settings.mapName);
        StartCoroutine(loadSceneAsync);
    }

    // Get the sprite that corresponds to the scene
    public Sprite GetSceneSprite(string spriteName) {
        StringBuilder path = new StringBuilder("Sprites/SceneSprites/");
        path.Append(spriteName);
        return Resources.Load<Sprite>(path.ToString());
    }

    private void LoadingScreen(SceneData.GameSettings settings) {
        loadingScreen.GetComponent<Image>().sprite =
            Resources.Load<Sprite>("Sprites/LoadingScreen/" + settings.spriteName);
        loadingScreen.GetComponent<LoadingScreen>().ScaleToFullScreen();
        loadingScreen.SetActive(true);
    }

    private IEnumerator LoadSceneAsync(string scene) {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone) {
            loadingScreen.GetComponent<LoadingScreen>().SetLoadingBar(asyncLoad.progress);
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
