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
    [SerializeField] private Canvas buttons;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private List<GameObject> unitPrefabs;

    // Start is called before the first frame update
    void Start()
    {
        InitPlayerDropdown();
        InitButtons();
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

    void InitButtons()
    {
        for (int i = 0; i < unitPrefabs.Count; i++)
        {
            GameObject button = Instantiate(buttonPrefab);
            button.transform.SetParent(buttons.transform);
            button.name = unitPrefabs[i].name;
            RectTransform buttonRectTransform = button.transform.GetComponent<RectTransform>();
            float width = buttonRectTransform.sizeDelta.x * buttonRectTransform.localScale.x;
            float height = buttonRectTransform.sizeDelta.y * buttonRectTransform.localScale.y;
            button.transform.localPosition = new Vector3(
                -(i % 3) * width,
                (i % 2) * height,
                0
            );

            button.GetComponent<Button>().onClick.AddListener(() => UnitButtonClick(button));

            button.GetComponent<Button>().GetComponentInChildren<Text>().text = button.name;
        }
    }

    void UnitButtonClick(GameObject button)
    {
        activePlayer.GetSpawnArea().SetEntityToSpawn(FindUnitByName(button.name));
        activePlayer.GetSpawnArea().ChangeGridWidth(1);
    }

    GameObject FindUnitByName(string name)
    {
        foreach (GameObject unit in unitPrefabs)
        {
            if (unit.name.Equals(name))
            {
                return unit;
            }
        }
        return null;
    }

    Player SetActivePlayer()
    {
        string nickname = playerSelect.options[playerSelect.value].text;
        foreach (Player player in players)
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
