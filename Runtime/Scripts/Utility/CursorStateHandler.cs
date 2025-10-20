using UnityEngine;
using UnityEngine.InputSystem;

namespace LucidityDrive.Extras
{
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
}
