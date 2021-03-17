using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> menus;
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

    public void Play(string scene) {
        LoadingScreen(scene);
        IEnumerator loadSceneAsync = LoadSceneAsync(scene);
        StartCoroutine(loadSceneAsync);
    }

    // Get the sprite that corresponds to the scene
    public Sprite GetSceneSprite(string scene) {
        StringBuilder path = new StringBuilder("Sprites/SceneSprites/");
        path.Append(scene);
        return Resources.Load<Sprite>(path.ToString());
    }

    private void LoadingScreen(string scene) {
        loadingScreen.transform.GetChild(1).gameObject.GetComponent<Image>().sprite = GetSceneSprite(scene);
        loadingScreen.SetActive(true);
    }

    private IEnumerator LoadSceneAsync(string scene) {
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
