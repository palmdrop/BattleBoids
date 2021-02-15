using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] private List<Player> players;
    [SerializeField] private Text boins;
    [SerializeField] private Dropdown playerSelect;
    [SerializeField] private Player activePlayer;

    // Start is called before the first frame update
    void Start()
    {
        InitPlayerDropdown();
        activePlayer = SetActivePlayer();
        UpdateBoins();
    }

    // Update is called once per frame
    void Update()
    {
        activePlayer = SetActivePlayer();
        UpdateBoins();
    }

    void InitPlayerDropdown()
    {
        playerSelect.ClearOptions();
        foreach (var player in players)
        {
            Dropdown.OptionData newPlayer = new Dropdown.OptionData();
            newPlayer.text = player.GetNickname();
            playerSelect.options.Add(newPlayer);
        }
    }

    Player SetActivePlayer()
    {
        string nickname = playerSelect.options[playerSelect.value].text;
        foreach (var player in players)
        {
            if (player.GetNickname().Equals(nickname))
            {
                return player;
            }
        }
        return null;
    }

    public void UpdateBoins()
    {
        boins.text = activePlayer.GetBoins().ToString();
    }

    public Player GetActivePlayer()
    {
        return activePlayer;
    }
}
