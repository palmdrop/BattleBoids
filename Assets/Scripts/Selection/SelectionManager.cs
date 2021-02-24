using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    private static List<GameObject> _selected = new List<GameObject>();

    public static void AddToSelected(GameObject entity)
    {
        entity.transform.GetChild(1).gameObject.SetActive(true);
        _selected.Add(entity);
    }

    public static void RemoveSelected()
    {
        foreach (GameObject entity in _selected)
        {
            entity.transform.GetChild(1).gameObject.SetActive(false);
        }

        _selected.Clear();
    }

    public static List<GameObject> GETSelectedEntities()
    {
        return _selected;
    }

}
