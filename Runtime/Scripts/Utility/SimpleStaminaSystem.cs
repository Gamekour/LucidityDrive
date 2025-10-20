using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LucidityDrive.Extras
{
    public class SimpleStaminaSystem : MonoBehaviour
    {
        public float staminaUseRateSprinting;
        public float staminaUseRateJumping;
        public float staminaRegenRate;
        public float staminaRegenDelay;
        public float staminaMax;
        public float staminaMinRequirement;
        public UnityEvent<float> onStaminaChange;
        private float stamina;
        private float timeSinceSprint;

        // Update is called once per frame
        void Update()
        {
            float previousStamina = stamina;

            bool considerJump = (
                PlayerInfo.isJumping &&
                staminaUseRateJumping != 0 &&
                !PlayerInfo.disableJump);

            bool considerSprint = (
                PlayerInfo.isSprinting &&
                staminaUseRateSprinting != 0 &&
                PlayerInfo.grounded &&
                LucidInputValueShortcuts.movement.magnitude > 0 &&
                !PlayerInfo.disableSprint);

            bool usingStamina = (considerJump || considerSprint);

            if (usingStamina)
            {
                float staminaUseRate = 0;
                if (considerSprint)
                    staminaUseRate += staminaUseRateSprinting;
                if (considerJump)
                    staminaUseRate += staminaUseRateJumping;
                stamina = Mathf.Clamp(stamina - (Time.deltaTime * staminaUseRate), 0, staminaMax);
                timeSinceSprint = 0;
            }
            else
                timeSinceSprint += Time.deltaTime;

            if (timeSinceSprint > staminaRegenDelay)
                stamina = Mathf.Clamp(stamina + (Time.deltaTime * staminaRegenRate), 0, staminaMax);

            if (!Mathf.Approximately(previousStamina, stamina) && onStaminaChange != null)
                onStaminaChange.Invoke(stamina / staminaMax);

            PlayerInfo.disableSprint = (stamina < staminaMinRequirement && staminaUseRateSprinting > 0);
            PlayerInfo.disableJump = (stamina < staminaMinRequirement && staminaUseRateJumping > 0);
        }
    }
}