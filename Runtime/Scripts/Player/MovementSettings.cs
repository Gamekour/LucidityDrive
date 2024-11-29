using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UntitledMovementSettings", menuName = "LucidityDrive/MovementSettings", order = 1)]
public class MovementSettings : ScriptableObject
{
    [Header("Gameplay")]
    public float movespeed;
    public float
        sprintmult,
        crawlspeed,
        crouchspeed,
        moveburst,
        slidemult,
        slidepushforce,
        jumpforcemult,
        jumpgrav,
        fallgrav,
        airmove,
        airdrag,
        maxAirAccel,
        flightforce,
        flightdrag,
        walkthreshold,
        directionaljumpmult,
        maxSlopeDefault,
        maxSlopeByYVelocity;

    [Header("Casting")]
    public float legWidth;
    public float 
        legWidthMult,
        down,
        airdownmult,
        probemult,
        moveflatprobemult,
        probeCutoffHeight,
        maxProbeOffset,
        probeXminimumOffset,
        probeZminimumOffset;

    [Header("Animation")]
    public float ratiomult;
    public float 
        ratiofreezethreshold,
        airtimemult,
        ratioBySpeed,
        maxlegmult;

    [Header("Physics")]
    public float friction;
    public float
        maxforcemult,
        hipRotationSpeed,
        hipspacesmoothness,
        hipspaceMaxRot,
        forcesmoothness,
        crouchmult,
        jumpmult,
        moveupmult,
        movedownmult,
        movedownclamp,
        crawlthreshold,
        crawlmult,
        wallruntilt,
        jumptilt,
        climbtilt,
        slidePushAngleThreshold;
}
