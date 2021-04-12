using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CommandManager : MonoBehaviour
{
    private static Dictionary<KeyCode, (Delegate, string)> _pressedActions;
    private static Dictionary<KeyCode, (Delegate, string)> heldActions;

    private void Awake()
    {
        _pressedActions = new Dictionary<KeyCode, (Delegate, string)>();
        heldActions = new Dictionary<KeyCode, (Delegate, string)>();
        
        RegisterPressedAction(KeyCode.H, PrintAllUsedKeys, "Prints all registered KeyCodes to the terminal");
    }

    private void Update()
    {
        foreach (KeyValuePair<KeyCode, (Delegate, string)> entry in _pressedActions.Where(
            entry => Input.GetKeyDown(entry.Key)))
        {
            entry.Value.Item1.DynamicInvoke();
        }

        foreach ( KeyValuePair<KeyCode, (Delegate, string)> entry in heldActions.Where(
            entry => Input.GetKey(entry.Key)))
        {
            entry.Value.Item1.DynamicInvoke();
        }
    }
    
    public static void RegisterPressedAction(KeyCode key, Action action, string description = "No description")
    {
        if (_pressedActions.ContainsKey(key))
        {
            _pressedActions[key] = (action, description);
            return;
        }

        _pressedActions.Add(key, (action, description));
    }

    public static void RegisterHeldAction(KeyCode key, Delegate action, string description = "No description")
    {
        if (heldActions.ContainsKey(key))
        {
            heldActions[key] = (action, description);
            return;
        }
        
        heldActions.Add(key, (action, description));
    }

    public void PrintAllUsedKeys()
    {
        StringBuilder result = new StringBuilder();

        foreach (var entry in _pressedActions)
        {
            result.Append("\n");
            result.Append("Press ");
            result.AppendFormat( entry.Key.ToString());
            result.Append(": ");
            result.Append(entry.Value.Item2);
        }

        result.Insert(0, "Registered: ");
        
        Debug.Log(result.ToString());
    }
   
}