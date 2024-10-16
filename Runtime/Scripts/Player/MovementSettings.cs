using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UntitledMovementSettings", menuName = "LucidityDrive/MovementSettings", order = 1)]
public class MovementSettings : ScriptableObject
{
    [Header("Gameplay")]
    public float 
        movespeed,
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
        directionaljumpmult;

    [Header("Casting")]
    public float 
        legWidth,
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
    public float 
        ratiomult,
        ratiofreezethreshold,
        airtimemult,
        ratioBySpeed,
        maxlegmult;

    [Header("Physics")]
    public float 
        friction,
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
        climbtilt;
}
