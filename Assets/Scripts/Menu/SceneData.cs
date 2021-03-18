using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public class SceneData : MonoBehaviour
{
    // Struct for holding campaign levels
    // NOTE This must hold: name == scene name == sprite name
    // Scene name is the name of the scene in /Assets/Scenes/
    // Sprite name is the name of the sprite to show in the campaign menu
    // Put the sprite in /Assests/Resources/Sprite/SceneSprites/
    public struct Level {
        public string description;
        public GameSettings gameSettings;
    }

    // Struct for holding multiplayer maps
    // NOTE This must hold: name == scene name == sprite name
    // Scene name is the name of the scene in /Assets/Scenes/
    // Sprite name is the name of the sprite to show in the multiplayer menu
    // Put the sprite in /Assests/Resources/Sprite/SceneSprites/
    public struct Map {
        public string name;
        public int numberOfPlayers;
    }

    // Struct for the settings to start the match with
    public struct GameSettings {
        public string mapName;
        public List<PlayerSettings> playerSettingsList;
        public Options options;
    }

    // Struct for a player's settings, used in GameSettings
    public struct PlayerSettings {
        public int id;
        public string nickname;
        public Color color;
    }

    // Struct with match options, used in GameSettings
    public struct Options {
        public int boins;
        public Dictionary<string, bool> units;
    }

    public enum Type {
        Campaign,
        Multiplayer
    }

    public static void SaveGameSettings(GameSettings gameSettings, Type type) {
        // Selected map
        PlayerPrefs.SetString("Scene", gameSettings.mapName);

        string prefix = type.ToString();

        // Player settings
        foreach (PlayerSettings playerSettings in gameSettings.playerSettingsList) {
            PlayerPrefs.SetString(
                prefix + "Player " + playerSettings.id.ToString(),
                playerSettings.nickname
            );
            PlayerPrefs.SetString(
                prefix + "Color " + playerSettings.id.ToString(),
                "#" + ColorUtility.ToHtmlStringRGBA(playerSettings.color)
            );
        }

        // Options
        PlayerPrefs.SetInt(prefix + "Boins", gameSettings.options.boins);
        foreach (KeyValuePair<string, bool> entry in gameSettings.options.units) {
            PlayerPrefs.SetString(prefix + entry.Key, entry.Value.ToString());
        }
    }

    // List with campaign levels
    public static readonly IList<Level> campaignLevels = new ReadOnlyCollection<Level>(
        new Level[] {
            new Level() { // NOTE Placeholder level TODO replace when real campaign level exists
                description = "There are no campaign levels yet. This is just a placeholder.",
                gameSettings = new GameSettings() {
                    mapName = "Dusk",
                    playerSettingsList = new List<PlayerSettings>(
                        new PlayerSettings[] {
                            new PlayerSettings() {
                                id = 1,
                                nickname = "You",
                                color = Color.blue
                            },
                            new PlayerSettings() {
                                id = 2,
                                nickname = "The enemy",
                                color = Color.red
                            }
                        }
                    ),
                    options = new Options {
                        boins = 1000,
                        units = new Dictionary<string, bool>() {
                            {"Commander", false},
                            {"Healer", false},
                            {"Hero", false},
                            {"Melee", true},
                            {"Ranged", true},
                            {"Scarecrow", false}
                        }
                    }
                }
            }
        }
    );

    // List with multiplayer maps
    public static readonly IList<Map> multiplayerMaps = new ReadOnlyCollection<Map>(
        new Map[] {
            new Map() {
                name = "Dusk",
                numberOfPlayers = 2
            }
        }
    );
}
