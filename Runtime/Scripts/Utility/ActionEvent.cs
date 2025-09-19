using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ActionEvent : MonoBehaviour
{
    public InputAction action;
    public UnityEvent<bool> actionStarted;
    public UnityEvent actionCancelled;
    private bool toggleValue = false;

    private void OnEnable()
    {
        action.Enable();
        action.started += ActionStarted;
        action.canceled += ActionCanceled;
    }

    private void OnDisable()
    {
        action.Disable();
        action.started -= ActionStarted;
        action.canceled -= ActionCanceled;
    }

    private void ActionStarted(InputAction.CallbackContext obj)
    {
        toggleValue = !toggleValue;
        actionStarted.Invoke(toggleValue);
    }

    private void ActionCanceled(InputAction.CallbackContext obj)
    {
        actionCancelled.Invoke();
    }
}
