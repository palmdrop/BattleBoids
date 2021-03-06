﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CommandManager : MonoBehaviour
{
    private Dictionary<KeyCode, (Delegate, bool, string)> _pressedActions;
    private Dictionary<KeyCode, (Delegate, bool, string)> _heldActions;
    private Dictionary<(KeyCode, KeyCode), (Delegate, bool, string)> _commandActions;
    public static CommandManager SharedInstance;

    private void Awake()
    {
        // We need two arrays, one for handling key down events
        _pressedActions = new Dictionary<KeyCode, (Delegate, bool, string)>();
        // ... the other to handle held down events
        _heldActions = new Dictionary<KeyCode, (Delegate, bool, string)>();

        _commandActions = new Dictionary<(KeyCode, KeyCode), (Delegate, bool, string)>();

        // To register a key, you pass in the key code, the function you want to run and a optional description
        // The description can be used to inform the user in-game
        RegisterPressedAction(KeyCode.H, PrintAllUsedKeys, "Prints all registered KeyCodes to the terminal");
        SharedInstance = this;
    }

    private void Update()
    {
        // Here we check if any of the pressed key actions are issued
        // If it is, invoke the function associated with that input
        foreach (KeyValuePair<KeyCode, (Delegate, bool, string)> entry in _pressedActions.Where(
            entry => Input.GetKeyDown(entry.Key)))
        {
            entry.Value.Item1.DynamicInvoke();
        }

        // Here we check if any of the held key actions are issued
        // If it is, invoke the function associated with that input
        foreach ( KeyValuePair<KeyCode, (Delegate, bool, string)> entry in _heldActions.Where(
            entry => Input.GetKey(entry.Key)))
        {
            entry.Value.Item1.DynamicInvoke();
        }

        foreach (KeyValuePair<(KeyCode, KeyCode), (Delegate, bool, string)> entry in _commandActions.Where(
            entry => Input.GetKey(entry.Key.Item1) && Input.GetKeyDown(entry.Key.Item2)))
        {
            entry.Value.Item1.DynamicInvoke();
        }
    }
    
    public void RegisterPressedAction(KeyCode key, Action action, string description = "No description", bool showAsTooltip = false)
    {
        // If the key is already associated with another action, overwrite it
        if (_pressedActions.ContainsKey(key))
        {
            _pressedActions[key] = (action, showAsTooltip, description);
            return;
        }

        // If not, add it to the action dictionary 
        _pressedActions.Add(key, (action, showAsTooltip, description));
    }

    // Similar to RegisterPressedActions, but for held actions instead. See above.
    public void RegisterHeldAction(KeyCode key, Delegate action, string description = "No description", bool showAsTooltip = false)
    {
        if (_heldActions.ContainsKey(key))
        {
            _heldActions[key] = (action, showAsTooltip, description);
            return;
        }
        
        _heldActions.Add(key, (action, showAsTooltip, description));
    }

    // Similar to RegisterPressedActions but enables a modifier key to be used.
    public void RegisterCommandAction(KeyCode modifierKey, KeyCode key, Action action, string description = "No description", bool showAsTooltip = false)
    {
        if (_commandActions.ContainsKey((modifierKey, key)))
        {
            _commandActions[(modifierKey,key)] = (action, showAsTooltip, description);
            return;
        }

        _commandActions.Add((modifierKey, key), (action, showAsTooltip, description));
    }

    // Returns a list of all keycodes (and the their description) that is going to be displayed as a button in-game
    public List<(KeyCode, string)> GETPressedKeyCodesAndDescription()
    {
        List<(KeyCode, string)> tooltipCommands = new List<(KeyCode, string)>();
        
        foreach (KeyValuePair<KeyCode, (Delegate, bool, string)> entry in _pressedActions)
        {
            if (entry.Value.Item2)
            {
                tooltipCommands.Add((entry.Key, entry.Value.Item3));
            }
        }
        
        return tooltipCommands;
    }

    // If a the key code have a function attached, it will run that function
    public void RunActionOnKeyCode(KeyCode keyCode, KeyCode modifier)
    {
        if (_commandActions.ContainsKey((modifier, keyCode)))
        {
            _commandActions[(modifier, keyCode)].Item1.DynamicInvoke();
        }
        else
        {
            if (_pressedActions.ContainsKey(keyCode))
            {
                _pressedActions[keyCode].Item1.DynamicInvoke();
            }

            if (_heldActions.ContainsKey(keyCode))
            {
                _heldActions[keyCode].Item1.DynamicInvoke();
            }
        }
    }

    // Prints all registered keys to the console together with their description.
    public void PrintAllUsedKeys()
    {
        StringBuilder result = new StringBuilder();

        foreach (var entry in _pressedActions)
        {
            AppendKeyAndDescription(result, entry.Key.ToString(), entry.Value.Item3, "Press");
        }
        
        foreach (var entry in _heldActions)
        {
            AppendKeyAndDescription(result, entry.Key.ToString(), entry.Value.Item3, "Hold");
        }

        foreach (var entry in _commandActions)
        {
            AppendKeyAndDescription(result, entry.Key.ToString(), entry.Value.Item3, "Commands");
        }

        result.Insert(0, "Registered: ");
        
        Debug.Log(result.ToString());
    }

    private void AppendKeyAndDescription(StringBuilder result, string key, string description, string actionType)
    {
        result.Append("\n");
        result.Append(actionType);
        result.Append(" ");
        result.AppendFormat(key);
        result.Append(": ");
        result.Append(description);
    }
   
}