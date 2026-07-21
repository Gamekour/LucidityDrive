using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LucidityDrive
{
    public class LDInputs : MonoBehaviour
    {
        public InputActionAsset ActionAsset;
        public bool restoreControlOnStart = true;

        private readonly Dictionary<InputAction, FieldInfo> actionValueMapping = new();
        private readonly List<InputAction> activeValueInputs = new();

        [SerializeField] CursorLockMode defaultLockMode;

        private void Awake() => Setup();

        private void OnEnable()
        {
            if (ActionAsset != null)
            {
                ActionAsset.Enable();
                if (restoreControlOnStart)
                    RestoreControl();
            }
        }

        private void OnDisable() => LucidInputActionRefs.mouseUnlock.started -= MouseLockToggle;

        private void Start()
        {
            LucidInputActionRefs.mouseUnlock.started += MouseLockToggle;
            SetState(defaultLockMode);
        }

        private void Update()
        {
            foreach (InputAction action in activeValueInputs)
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
                action.Enable();
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
                if (actionValueMapping.ContainsKey(action))
                    InitializeShortcutValue(action);
            }
            PlayerInfo.OnInputsReady.Invoke();
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

        private void InitializeShortcutValue(InputAction action)
        {
            FieldInfo valueField = actionValueMapping[action];
            Type fieldType = valueField.FieldType;

            if (fieldType == typeof(Vector2))
            {
                valueField.SetValue(this, Vector2.zero);
            }
            else if (fieldType == typeof(bool))
            {
                valueField.SetValue(this, false);
            }
            else
            {
                valueField.SetValue(this, null);
            }
        }

        public void RevokeControl() => ActionAsset.actionMaps[0].Disable();
        public void RestoreControl() => ActionAsset.actionMaps[0].Enable();

        private void MouseLockToggle(InputAction.CallbackContext obj)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                SetState(CursorLockMode.None);
            else
                SetState(CursorLockMode.Locked);
        }

        public void SetState(CursorLockMode state)
        {
            Cursor.lockState = state;
            PlayerInfo.headLocked = (state == CursorLockMode.Locked);
        }

        public void SetState(string statename)
        {
            switch (statename.ToLower())
            {
                case "none":
                    SetState(CursorLockMode.None);
                    break;
                case "locked":
                    SetState(CursorLockMode.Locked);
                    break;
                case "confined":
                    SetState(CursorLockMode.Confined);
                    break;
                default:
                    SetState(CursorLockMode.None);
                    break;
            }
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
            mouseUnlock,
            freeLook,
            spin
            ;
    }

    //used to easily access values from the input actions above without constantly calling readvalue
    public static class LucidInputValueShortcuts
    {
        public static Vector2
            movement,
            headLook
            ;

        public static bool
            jump,
            crouch,
            sprint,
            slide,
            bslide,
            grabL,
            grabR,
            freeLook,
            spin
            ;
    }
}