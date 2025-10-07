using UnityEngine;

[CreateAssetMenu(fileName = "UntitledMovementSettings", menuName = "LucidityDrive/MovementSettings", order = 1)]
public class MovementSettings : ScriptableObject
{
    [Header("Simple")]
    [Tooltip("How fast the player should move")]
    public float moveSpeed;
    [Tooltip("How fast the player should move when crawling")]
    public float moveSpeedCrawling;
    [Tooltip("How fast the player should move when crouched")]
    public float moveSpeedCrouched;
    [Tooltip("Multiply the speed by this when sprinting")]
    public float sprintScale;
    [Tooltip("Apply extra force when changing directions, scaled by this value")]
    public float moveBurst;
    [Tooltip("How much force to apply from legs when sliding on the ground")]
    public float slidePushStrength;
    [Tooltip("Vertical force multiplier while jumping")]
    public float jumpForceScale;
    [Tooltip("Gravity applied to player while jumping")]
    public float jumpGravity;
    [Tooltip("Gravity applied to player while falling")]
    public float fallGravity;
    [Tooltip("Friction multiplier of the player's feet")]
    public float friction;
    [Tooltip("How fast you move in the air")]
    public float aerialMovementSpeed;
    [Tooltip("Drag multiplier in the air")]
    public float aerialDrag;
    [Tooltip("Maximum air movement speed, air strafing can bypass this")]
    public float maxAirAcceleration;
    [Tooltip("Makes air strafing easier")]
    public float airTurnAssist;
    [Tooltip("How fast you move when flying")]
    public float flightSpeed;
    [Tooltip("How much drag to apply when flying")]
    public float flightDrag;
    [Tooltip("Amount of horizontal movement at which speed starts to reduce")]
    public float strafeWalkStartThreshold;
    [Tooltip("Strafe speed multiplier at peak (directly left or right)")]
    public float strafeWalkSpeedMult;
    [Tooltip("How much the legs will push horizontally from a jump")]
    public float directionalJumpStrength;
    [Tooltip("Horizontal boost to apply when jumping")]
    public float directionalJumpBoost;
    [Tooltip("How much \"magnetic\" force to apply into a surface, scaled by its slope")]
    public float surfaceMagnetismBySlope;
    [Tooltip("Cutoff between slopes that can be walked on normally and slopes that will only be walked on if holding space")]
    public float highSlopeThreshold;

    [Header("Advanced")]
    [Tooltip("How far down the floor probe will try to extend")]
    public float probeDepth;
    [Tooltip("How far out the floor probe will try to extend")]
    public float probeScale;
    [Tooltip("Furthest out that the probe can extend")]
    public float maxProbeOffset;
    [Tooltip("Minimum horizontal offset of x-axis floor probes")]
    public float probeXMinimumOffset;
    [Tooltip("Minimum horizontal offset of z-axis floor probes")]
    public float probeZMinimumOffset;
    [Tooltip("Correction force to reapply traction")]
    public float footSlideStrength;
    [Tooltip("Maximum force the legs will apply in push calculations")]
    public float maxForceScale;
    [Tooltip("How smoothly the legs will attempt to reach the target leg extension")]
    public float forceSmoothness;
    [Tooltip("Torque to apply to the pelvis to try to match the player's head")]
    public float pelvisRotationSpeed;
    [Tooltip("Smoothing value for hipspace rotation")]
    public float hipSpaceRotationSmoothness;
    [Tooltip("Scale the target height by how steeply you're trying to move up a slope, scaled by this value")]
    public float targetHeightByPositiveSlope;
    [Tooltip("Scale the target height by how steeply you're trying to move down a slope, scaled by this value")]
    public float targetHeightByNegativeSlope;
    [Tooltip("Clamp the height reduction from negative slope movement to this value")]
    public float targetHeightByNegativeSlopeClamp;
    [Tooltip("Lean away from steep surfaces by this much (normally)")]
    public float slopeTilt;
    [Tooltip("Lean away from steep surfaces by this much (while jumping)")]
    public float jumpTilt;
    [Tooltip("Lean away from steep surfaces by this much (while climbing)")]
    public float climbtilt;
    [Tooltip("At this angle, don't push off of the surface while sliding")]
    public float slidePushAngleThreshold;
    [Tooltip("Scale the target height by this value")]
    public float targetHeightScale;
}
