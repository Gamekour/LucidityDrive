using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorStateHandler : MonoBehaviour
{
    [SerializeField] CursorLockMode defaultLockMode;

    private void OnDisable()
    {
        LucidInputActionRefs.mouseUnlock.started -= MouseLockToggle;
    }

    private void Start()
    {
        LucidInputActionRefs.mouseUnlock.started += MouseLockToggle;
        SetState(defaultLockMode);
    }

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
        PlayerInfo.headlocked = (state == CursorLockMode.Locked);
    }
}
