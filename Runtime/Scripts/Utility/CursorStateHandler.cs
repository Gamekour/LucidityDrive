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
            SetState(0);
        else
            SetState(2);
    }

    public void SetState(int state)
    {
        switch (state)
        {
            case 0:
                Cursor.lockState = CursorLockMode.None;
                PlayerInfo.headlocked = false;
                break;
            case 1:
                Cursor.lockState = CursorLockMode.Confined;
                PlayerInfo.headlocked = false;
                break;
            case 2:
                Cursor.lockState = CursorLockMode.Locked;
                PlayerInfo.headlocked = true;
                break;
            default:
                Debug.LogError("invalid lock state");
                break;
        }
    }
}
