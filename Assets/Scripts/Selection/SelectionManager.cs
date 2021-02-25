using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    private static List<Selectable> _selected = new List<Selectable>();

    public static void AddToSelected(Selectable selected)
    {
        _selected.Add(selected);
    }

    public static void RemoveSelected()
    {
        foreach (Selectable selected in GETSelectedEntities())
        {
            selected.SetSelectionIndicator(false);
        }
        
        _selected.Clear();
    }

    public static List<Selectable> GETSelectedEntities()
    {
        return _selected;
    }

}
