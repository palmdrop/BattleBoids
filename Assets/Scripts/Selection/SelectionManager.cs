using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    private static List<GameObject> _selected = new List<GameObject>();

    public static void AddToSelected(GameObject entity)
    {
        _selected.Add(entity);
    }

    public static void RemoveSelected()
    {

        _selected.Clear();
    }

    public static List<GameObject> GETSelectedEntities()
    {
        return _selected;
    }

}
