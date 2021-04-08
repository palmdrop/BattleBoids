using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CommandManager : MonoBehaviour
{
    private static Dictionary<string, Delegate> pressedActions = new Dictionary<string, Delegate>();
    private static Dictionary<string, Delegate> heldActions = new Dictionary<string, Delegate>();

    private void Update()
    {
        foreach (KeyValuePair<string, Delegate> entry in pressedActions.Where(
            entry => Input.GetKeyDown(entry.Key)))
        {
            entry.Value.DynamicInvoke();
        }

        foreach ( KeyValuePair<string, Delegate> entry in heldActions.Where(
            entry => Input.GetKey(entry.Key)))
        {
            entry.Value.DynamicInvoke();
        }
    }
    
    public static void RegisterPressedAction(String key, Action action)
    {
        if (pressedActions.ContainsKey(key))
        {
            pressedActions[key] = action;
            return;
        }

        pressedActions.Add(key, action);
    }

    public static void RegisterHeldAction(String key, Delegate action)
        {
            if (heldActions.ContainsKey(key))
            {
                heldActions[key] = action;
                return;
            }
            
            heldActions.Add(key, action);
        }
   
}