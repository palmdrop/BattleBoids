using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneData : MonoBehaviour
{
    // Struct for holding campaign levels
    // Scene name is the name of the scene in /Assets/Scenes/
    // Sprite name is the name of the sprite to show in the campaign menu
    // Put the sprite in /Assests/Resources/Sprite/SceneSprites/
    public struct Level {
        public string description;
        public GameSettings gameSettings;
    }

    // Struct for holding multiplayer maps
    // Scene name is the name of the scene in /Assets/Scenes/
    // Sprite name is the name of the sprite to show in the multiplayer menu
    // Put the sprite in /Assests/Resources/Sprite/SceneSprites/
    public struct Map {
        public string name;
    }

    // Struct for the settings to start the match with
    public struct GameSettings {
        public string mapName;
        public string spriteName;
        public List<PlayerSettings> playerSettingsList;
        public Options options;
    }

    // Struct for a player's settings, used in GameSettings
    public struct PlayerSettings {
        public int id;
        public string nickname;
        public Color color;
        public int boins;
    }

    // Struct with match options, used in GameSettings
    public struct Options {
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
            PlayerPrefs.SetInt(
                prefix + "Boins " + playerSettings.id.ToString(),
                playerSettings.boins
            );
        }

        // Options
        foreach (KeyValuePair<string, bool> entry in gameSettings.options.units) {
            PlayerPrefs.SetString(prefix + entry.Key, entry.Value.ToString());
        }
    }

    // List with campaign levels
    public static readonly IList<Level> campaignLevels = new ReadOnlyCollection<Level>(
        new Level[] {
            new Level() {
                description = "Level One. Start your journey here!",
                gameSettings = new GameSettings() {
                    mapName = "Dusk1",
                    spriteName = "Dusk",
                    playerSettingsList = new List<PlayerSettings>(
                        new PlayerSettings[] {
                            new PlayerSettings() {
                                id = 1,
                                nickname = "You",
                                color = Color.blue,
                                boins = 1000
                            },
                            new PlayerSettings() {
                                id = 2,
                                nickname = "The enemy",
                                color = Color.red,
                                boins = 1000
                            }
                        }
                    ),
                    options = new Options {
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
            },
            new Level() {
                description = "Level Two. Time to start thinking!",
                gameSettings = new GameSettings() {
                    mapName = "Dusk2",
                    spriteName = "Dusk",
                    playerSettingsList = new List<PlayerSettings>(
                        new PlayerSettings[] {
                            new PlayerSettings() {
                                id = 1,
                                nickname = "You",
                                color = Color.blue,
                                boins = 1000
                            },
                            new PlayerSettings() {
                                id = 2,
                                nickname = "The enemy",
                                color = Color.red,
                                boins = 1000
                            }
                        }
                    ),
                    options = new Options {
                        units = new Dictionary<string, bool>() {
                            {"Commander", false},
                            {"Healer", false},
                            {"Hero", true},
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
            }, 
            new Map()
            {
                name = "Midnight", 
            }
        }
    );

    public static Level? GetNextLevel()
    {
        int next = 0;
        foreach (var level in SceneData.campaignLevels) {
            next += 1;
            if (level.gameSettings.mapName == SceneManager.GetActiveScene().name)
                break;
        }
        if (next == SceneData.campaignLevels.Count) {
            return null;
        }
        return SceneData.campaignLevels[next];
    }
}
