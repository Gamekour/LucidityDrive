using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UntitledMovementSettings", menuName = "motif/MovementSettings", order = 1)]
public class MovementSettings : ScriptableObject
{
    //ik this isn't great but it's a lot less of a headache than "oh yeah go grab the Params[31] real quick"

    public float legWidth;
    public float legWidthMult;
    public float ratiomult;
    public float ratiofreezethreshold;
    public float hipRotationSpeed;
    public float down;
    public float maxforcemult;
    public float forcesmoothness;
    public float maxProbeOffset;
    public float maxlegmult;
    public float crouchmult;
    public float jumpmult;
    public float jumpforcemult;
    public float probemult;
    public float movespeed;
    public float friction;
    public float slidemult;
    public float wallruntilt;
    public float airdownmult;
    public float moveflatprobemult;
    public float probeXminimumOffset;
    public float probeZminimumOffset;
    public float hipspaceMaxRot;
    public float airtimemult;
    public float moveupmult;
    public float movedownmult;
    public float movedownclamp;
    public float hipspacesmoothness;
    public float moveburst;
    public float crawlthreshold;
    public float crawlspeed;
    public float crouchspeed;
    public float crawlmult;
    public float flightforce;
    public float flightdrag;
    public float sprintmult;
    public float directionaljumpmult;
    public float jumptilt;
    public float slidepushforce;
    public float climbtilt;
    [Header("Special")]
    public float timescale;
    public float jumpgrav;
    public float fallgrav;
    public float airmove;
    public float airdrag;
}
