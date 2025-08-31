using UnityEngine;

[CreateAssetMenu(fileName = "UntitledMovementSettings", menuName = "LucidityDrive/MovementSettings", order = 1)]
public class MovementSettings : ScriptableObject
{
    [Header("Gameplay")]
    public float moveSpeed;
    public float
        sprintScale,
        moveSpeedCrawling,
        moveSpeedCrouched,
        moveBurst,
        footSlideStrength,
        slidePushStrength,
        jumpForceScale,
        jumpGravity,
        fallGravity,
        aerialMovementSpeed,
        aerialDrag,
        maxAirAcceleration,
        airTurnAssist,
        flightSpeed,
        flightDrag,
        strafeWalkAngularThreshold,
        strafeWalkSpeedMult,
        directionalJumpStrength,
        surfaceMagnetismBySlope,
        highSlopeThreshold;

    [Header("Casting")]
    public float legWidth;
    public float
        probeDepth,
        probeScale,
        maxProbeOffset,
        probeXMinimumOffset,
        probeZMinimumOffset;

    [Header("Physics")]
    public float friction;
    public float
        maxForceScale,
        pelvisRotationSpeed,
        hipSpaceRotationSmoothness,
        hipSpaceMaxRotation,
        forceSmoothness,
        jumpHeightScale,
        targetHeightByPositiveSlope,
        targetHeightByNegativeSlope,
        targetHeightByNegativeSlopeClamp,
        maxCrawlSpeed,
        crawlHeight,
        slopeTilt,
        jumpTilt,
        climbtilt,
        slidePushAngleThreshold;
}
