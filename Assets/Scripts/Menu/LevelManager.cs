using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GameObject dropdown;

    private Dropdown _dd;
    private Text _name;
    private Image _image;
    private Text _description;

    // Struct for holding campaign levels
    // NOTE This must hold: name == scene name == sprite name
    // Scene name is the name of the scene in /Assets/Scenes/
    // Sprite name is the name of the sprite to show in the campaign menu
    // Put the sprite in /Assests/Resources/Sprite/LevelSprites/
    public struct Level {
        public string name;
        public string description;
    }

    private List<Level> _levels = new List<Level>();

    void Start() {
        _dd = dropdown.GetComponent<Dropdown>();
        _name = transform.Find("Name").gameObject.GetComponent<Text>();
        _image = transform.Find("Image").gameObject.GetComponent<Image>();
        _description = transform.Find("Description").gameObject.GetComponent<Text>();

        AddLevels();
        InitDropdownOptions();
        UpdateLevelHolder();
    }

    // Add levels to the list of levels
    private void AddLevels() {
        // NOTE: Temp code. No levels exists yet -> use LevelOne scene
        _levels.Add(
            new Level {
                name = "LevelOne",
                description = "There are no campaign levels yet. This is just a placeholder."
            }
        );
    }

    // Get the sprite that corresponds to the level name
    private Sprite GetLevelSprite(string levelName) {
        StringBuilder path = new StringBuilder("Sprites/LevelSprites/");
        path.Append(levelName);
        return Resources.Load<Sprite>(path.ToString());
    }

    // Init Dropdown with levels
    private void InitDropdownOptions() {
        _dd.ClearOptions();
        Dropdown.OptionData option;
        foreach (Level level in _levels) {
            option = new Dropdown.OptionData();
            option.text = level.name;
            _dd.options.Add(option);
        }
        _dd.captionText.text = _levels[_dd.value].name;
    }

    // Set level holder to display selected level in dropdown
    public void UpdateLevelHolder() {
        Level selected = _levels[_dd.value];
        _name.text = selected.name;
        _image.sprite = GetLevelSprite(selected.name);
        _description.text = selected.description;
    }

    // Start the selected level
    public void Play() {
        SceneManager.LoadScene(_levels[_dd.value].name);
    }
}
