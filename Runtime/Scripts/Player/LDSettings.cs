using UnityEngine;

namespace LucidityDrive
{
    [CreateAssetMenu(fileName = "UntitledSettings", menuName = "LucidityDrive/Settings", order = 1)]
    public class LDSettings : ScriptableObject
    {
        [Header("Simple")]
        [Tooltip("How fast the player should move")]
        public float moveSpeed = 2;
        [Tooltip("How fast the player should move when crawling")]
        public float moveSpeedCrawling = 1;
        [Tooltip("How fast the player should move when crouched")]
        public float moveSpeedCrouched = 1.5f;
        [Tooltip("Multiply the speed by this when sprinting")]
        public float sprintScale = 3;
        [Tooltip("Apply extra force when changing directions, scaled by this value")]
        public float moveBurst = 40;
        [Tooltip("How much force to apply from legs when sliding on the ground")]
        public float slidePushStrength = 16;
        [Tooltip("Vertical force multiplier while jumping")]
        public float jumpForceScale = 1;
        [Tooltip("Gravity applied to player while jumping")]
        public float jumpGravity = -9.81f;
        [Tooltip("Gravity applied to player while falling")]
        public float fallGravity = -9.81f;
        [Tooltip("Friction multiplier of the player's feet")]
        public float friction = 0.45f;
        [Tooltip("How fast you move in the air")]
        public float aerialMovementSpeed = 0;
        [Tooltip("Drag to apply while midair")]
        public float aerialDrag = 0;
        [Tooltip("Maximum air movement speed, air strafing can bypass this")]
        public float maxAirAcceleration = 0;
        [Tooltip("Makes air strafing easier")]
        public float airTurnAssist = 0;
        [Tooltip("How fast you move when flying")]
        public float flightSpeed = 10;
        [Tooltip("How much drag to apply when flying")]
        public float flightDrag = 0.05f;
        [Tooltip("Amount of horizontal movement at which speed starts to reduce")]
        public float strafeWalkStartThreshold = 0.5f;
        [Tooltip("Strafe speed multiplier at peak (directly left or right)")]
        public float strafeWalkSpeedMult = 0.75f;
        [Tooltip("How much the legs will push horizontally from a jump")]
        public float directionalJumpStrength = 6;
        [Tooltip("Horizontal boost to apply when jumping")]
        public float directionalJumpBoost = 0;
        [Tooltip("How much \"magnetic\" force to apply into a surface, scaled by its slope")]
        public float surfaceMagnetismBySlope = 0;
        [Tooltip("Cutoff between slopes that can be walked on normally and slopes that will only be walked on if holding space")]
        public float highSlopeThreshold = 0.5f;
        [Tooltip("Force to apply vertically when holding the slide button")]
        public float slideBoostVertical = 0;
        [Tooltip("Force to apply in the movement direction when holding the slide button")]
        public float slideBoostHorizontal = 0;

        [Header("Advanced")]
        [Tooltip("How far down the floor probe will try to extend")]
        public float probeDepth = 2;
        [Tooltip("How far out the floor probe will try to extend")]
        public float probeScale = 1;
        [Tooltip("Furthest out that the probe can extend")]
        public float maxProbeOffset = 3;
        [Tooltip("Minimum horizontal offset of x-axis floor probes")]
        public float probeXMinimumOffset = 0.1f;
        [Tooltip("Minimum horizontal offset of z-axis floor probes")]
        public float probeZMinimumOffset = 0.1f;
        [Tooltip("Correction force to reapply traction")]
        public float footSlideStrength = 14;
        [Tooltip("Maximum force the legs will apply in push calculations")]
        public float maxForceScale = 7;
        [Tooltip("How smoothly the legs will attempt to reach the target leg extension")]
        public float forceSmoothness = 20;
        [Tooltip("Torque to apply to the pelvis to try to match the player's head")]
        public float pelvisRotationSpeed = 2000;
        [Tooltip("Scale the target height by how steeply you're trying to move up a slope, scaled by this value")]
        public float targetHeightByPositiveSlope = 0;
        [Tooltip("Scale the target height by how steeply you're trying to move down a slope, scaled by this value")]
        public float targetHeightByNegativeSlope = 0.25f;
        [Tooltip("Clamp the height reduction from negative slope movement to this value")]
        public float targetHeightByNegativeSlopeClamp = 2;
        [Tooltip("Lean away from steep surfaces by this much (normally)")]
        public float slopeTilt = 0.1f;
        [Tooltip("Lean away from steep surfaces by this much (while jumping)")]
        public float jumpTilt = 0.15f;
        [Tooltip("Lean away from steep surfaces by this much (while climbing)")]
        public float climbTilt = 0.3f;
        [Tooltip("At this angle, don't push off of the surface while sliding")]
        public float slidePushAngleThreshold = 45;
        [Tooltip("Scale the target height by this value")]
        public float targetHeightScale = 0.91f;
        [Tooltip("Below this height, calculate push as if legs were still at this height")]
        public float minLegAdjust = 0.2f;
        [Tooltip("Adjust the target leg length while climbing")]
        public float climbLegAdjust = 0.3f;
        [Tooltip("Maximum angle for freelook before hips start to rotate anyways")]
        public float maxFreeLookAngle = 60;
    }
}