using UnityEngine;

namespace LucidityDrive
{
    [CreateAssetMenu(fileName = "UntitledArmSettings", menuName = "LucidityDrive/ArmSettings", order = 1)]
    public class ArmSettings : ScriptableObject
    {
        [Tooltip("Maximum distance of grab cast (scaled by arm length)")]
        public float castDistance = 1.05f;
        [Tooltip("Maximum distance of generated arm joint (scaled by arm length)")]
        public float limitDistance = 1.1f;
        [Tooltip("Width of first climb cast (shoulder to surface)")]
        public float firstCastWidth = 0.075f;
        [Tooltip("Width of second climb cast (gap detection)")]
        public float secondCastWidth = 0.025f;
        [Tooltip("Minimum normal.y for a surface to be grabbable as the top of a ledge")]
        public float minDowncastNrmY = 0.8f;
        [Tooltip("Additional force when letting go of a ledge")]
        public float ungrabBoost = 50;
        [Tooltip("Attempt to use force to help keep lucid tools up to speed with the player. Only necessary if player moves really fast.")]
        public float velocityCheatForLucidTools = 0;
        [Tooltip("Reduce horizontal movement while swinging")]
        public float swingStabilization = 0.5f;
        [Tooltip("Speed at which target climb height changes when pulling or lowering")]
        public float pullSpeed = 4;
        [Tooltip("Reduce wobble when pulling or lowering")]
        public float pullDamp = 1;
        [Tooltip("Maximum height the arms will try to pull to, relative to grab position")]
        public float maxPullHeight = 3;
        [Header("Experimental")]
        [Tooltip("Vertical force threshold to activate climbing logic while grabbing a physics object; very buggy and can cause animation softlocks. set to Infinity to disable")]
        public float climbModeForceThreshold = Mathf.Infinity;
    }
}