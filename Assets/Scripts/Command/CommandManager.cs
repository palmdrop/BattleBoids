using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CommandManager : MonoBehaviour
{
    private static Dictionary<KeyCode, Delegate> _pressedActions;
    private static Dictionary<KeyCode, Delegate> heldActions;

    private void Awake()
    {
        _pressedActions = new Dictionary<KeyCode, Delegate>();
        heldActions = new Dictionary<KeyCode, Delegate>();
        Debug.Log("CAlled first");
        RegisterPressedAction(KeyCode.H, PrintAllUsedKeys);
    }

    private void Update()
    {
        foreach (KeyValuePair<KeyCode, Delegate> entry in _pressedActions.Where(
            entry => Input.GetKeyDown(entry.Key)))
        {
            entry.Value.DynamicInvoke();
        }

        foreach ( KeyValuePair<KeyCode, Delegate> entry in heldActions.Where(
            entry => Input.GetKey(entry.Key)))
        {
            entry.Value.DynamicInvoke();
        }
    }
    
    public static void RegisterPressedAction(KeyCode key, Action action)
    {
        if (_pressedActions.ContainsKey(key))
        {
            _pressedActions[key] = action;
            return;
        }

        _pressedActions.Add(key, action);
    }

    public static void RegisterHeldAction(KeyCode key, Delegate action)
    {
        if (heldActions.ContainsKey(key))
        {
            heldActions[key] = action;
            return;
        }
        
        heldActions.Add(key, action);
    }

    public void PrintAllUsedKeys()
    {
        StringBuilder result = new StringBuilder();

        foreach (var entry in _pressedActions)
        {
            result.AppendFormat( entry.Key.ToString());
            result.Append(" ");
        }

        result.Insert(0, "Registered: ");
        
        Debug.Log(result.ToString());
    }
   
}