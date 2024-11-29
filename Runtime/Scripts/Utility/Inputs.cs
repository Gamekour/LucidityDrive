using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class Inputs : MonoBehaviour
{
    [SerializeField] InputActionAsset m_ActionAsset;
    public InputActionAsset ActionAsset { get => m_ActionAsset; set => m_ActionAsset = value; }

    private readonly Dictionary<InputAction, FieldInfo> actionValueMapping = new();
    private readonly List<InputAction> activeValueInputs = new();

    private void Awake()
    {
        Setup();
    }

    private void OnEnable()
    {
        if (m_ActionAsset != null) m_ActionAsset.Enable();
    }

    private void Update()
    {
        foreach(InputAction action in activeValueInputs)
        {
            UpdateShortcutValue(action);
        }
    }

    public void Setup()
    {
        Type targetType = typeof(LucidInputActionRefs);
        Type actionValueType = typeof(LucidInputValueShortcuts);
        FieldInfo[] fields = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        foreach (var field in fields)
        {
            InputAction action = ActionAsset.FindAction(field.Name, true);
            FieldInfo valueField = actionValueType.GetField(field.Name);
            if (valueField != null)
            {
                actionValueMapping.Add(action, valueField);
                switch (action.type)
                {
                    case InputActionType.Button:
                        action.started += Button_Update;
                        action.canceled += Button_Update;
                        break;
                    case InputActionType.Value:
                        action.started += Value_Update;
                        action.canceled += Value_Update;
                        break;
                    default: //not sure how passthroughs work but i'll eventually add logic for them here
                        return;
                }
            }
            field.SetValue(this, action);
        }
    }

    //generic event for setting the input value shortcut when the action is modified
    private void Button_Update(InputAction.CallbackContext obj)
    {
        FieldInfo valueField = actionValueMapping[obj.action];
        valueField.SetValue(this, !obj.canceled);
    }

    private void Value_Update(InputAction.CallbackContext obj)
    {
        UpdateShortcutValue(obj.action);
        if (obj.canceled)
        {
            activeValueInputs.Remove(obj.action);
        }
        else
            activeValueInputs.Add(obj.action);
    }

    private void UpdateShortcutValue(InputAction action)
    {
        FieldInfo valueField = actionValueMapping[action];
        object value = action.ReadValueAsObject();
        valueField.SetValue(this, value);
    }
}

//used to easily access relevant input actions for events
public static class LucidInputActionRefs
{
    public static InputAction
        movement,
        jump,
        crouch,
        sprint,
        headLook,
        slide,
        bslide,
        mousePos,
        grabL,
        grabR,
        dropL,
        dropR,
        camSelect1,
        camSelect2,
        camSelect3,
        camSelect4,
        camCycle,
        mouseUnlock;
}

//used to easily access values from the input actions above without constantly calling readvalue
public static class LucidInputValueShortcuts
{
    public static Vector2 
        movement, 
        headLook, 
        mousePos;

    public static bool 
        jump,
        crouch,
        sprint,
        slide,
        bslide,
        grabL,
        grabR;
}
