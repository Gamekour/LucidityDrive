using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UntitledMovementSettings", menuName = "motif/MovementSettings", order = 1)]
public class MovementSettings : ScriptableObject
{
    //ik this isn't great but it's a lot less of a headache than "oh yeah go grab the Params[31] real quick"

    [Header("Gameplay")]
    public float movespeed;
    public float sprintmult;
    public float crawlspeed;
    public float crouchspeed;
    public float moveburst;
    public float slidemult;
    public float slidepushforce;
    public float jumpforcemult;
    public float jumpgrav;
    public float fallgrav;
    public float airmove;
    public float airdrag;
    public float flightforce;
    public float flightdrag;
    public float timescale;
    public float walkthreshold;

    [Header("Casting")]
    public float legWidth;
    public float legWidthMult;
    public float down;
    public float probemult;
    public float moveflatprobemult;
    public float maxProbeOffset;
    public float probeXminimumOffset;
    public float probeZminimumOffset;

    [Header("Animation")]
    public float ratiomult;
    public float ratiofreezethreshold;
    public float airtimemult;

    [Header("Physics")]
    public float friction;
    public float maxforcemult;
    public float hipRotationSpeed;
    public float hipspacesmoothness;
    public float hipspaceMaxRot;
    public float forcesmoothness;
    public float maxlegmult;
    public float crouchmult;
    public float jumpmult;
    public float airdownmult;
    public float moveupmult;
    public float movedownmult;
    public float movedownclamp;
    public float crawlthreshold;
    public float crawlmult;
    public float directionaljumpmult;
    public float wallruntilt;
    public float jumptilt;
    public float climbtilt;
}
