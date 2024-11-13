using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class LucidWorldInfo : MonoBehaviour
{
    [SerializeField] LayerMask geometryMaskLayers;
    private void Awake()
    {
        Shortcuts.geometryMask = geometryMaskLayers;
    }
}

public static class Shortcuts
{
    public static int geometryMask = 0; //determines which layers should be considered for physics casting in most Lucidity Drive scripts

    public readonly static List<HumanBodyBones> hb2list = new List<HumanBodyBones> {
        HumanBodyBones.Spine,
        HumanBodyBones.Chest,
        HumanBodyBones.Neck,
        HumanBodyBones.Head,
        HumanBodyBones.LeftShoulder,
        HumanBodyBones.RightShoulder,
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.LeftHand,
        HumanBodyBones.RightHand,
        HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.RightUpperLeg,
        HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.RightLowerLeg,
        HumanBodyBones.LeftFoot,
        HumanBodyBones.RightFoot
    };

    public readonly static List<HumanBodyBones> hb2list_full = new List<HumanBodyBones>()
    {
        HumanBodyBones.Hips,
        HumanBodyBones.Spine,
        HumanBodyBones.Chest,
        HumanBodyBones.Neck,
        HumanBodyBones.Head,
        HumanBodyBones.LeftShoulder,
        HumanBodyBones.RightShoulder,
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.LeftHand,
        HumanBodyBones.RightHand,
        HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.RightUpperLeg,
        HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.RightLowerLeg,
        HumanBodyBones.LeftFoot,
        HumanBodyBones.RightFoot
    };

    public static readonly Dictionary<HumanBodyBones, string> boneNames =
    Enum.GetValues(typeof(HumanBodyBones))
        .Cast<HumanBodyBones>()
        .ToDictionary(bone => bone, bone => Enum.GetName(typeof(HumanBodyBones), bone));

    //returns shortest possible rotation between two quaternions
    public static Quaternion QShortestRotation(Quaternion a, Quaternion b)
    {
        if (Quaternion.Dot(a, b) < 0)
            return a * Quaternion.Inverse(QMultiply(b, -1));

        else return a * Quaternion.Inverse(b);
    }

    //scales a quaternion on all axes
    public static Quaternion QMultiply(Quaternion input, float scalar)
    {
        return new Quaternion(input.x * scalar, input.y * scalar, input.z * scalar, input.w * scalar);
    }

    //returns the next bone in succession in a humanoid rig (removing ambiguity for bones with multiple children) - used mostly for calculating bone lengths
    public static HumanBodyBones PrimaryChild(HumanBodyBones parent)
    {
        switch (parent)
        {
            case HumanBodyBones.Hips:
                return HumanBodyBones.Spine;
            case HumanBodyBones.Spine:
                return HumanBodyBones.Chest;
            case HumanBodyBones.Chest:
                return HumanBodyBones.Neck;
            case HumanBodyBones.Neck:
                return HumanBodyBones.Head;
            case HumanBodyBones.RightShoulder:
                return HumanBodyBones.RightUpperArm;
            case HumanBodyBones.RightUpperArm:
                return HumanBodyBones.RightLowerArm;
            case HumanBodyBones.RightLowerArm:
                return HumanBodyBones.RightHand;
            case HumanBodyBones.LeftShoulder:
                return HumanBodyBones.LeftUpperArm;
            case HumanBodyBones.LeftUpperArm:
                return HumanBodyBones.LeftLowerArm;
            case HumanBodyBones.LeftLowerArm:
                return HumanBodyBones.LeftHand;
            case HumanBodyBones.LeftUpperLeg:
                return HumanBodyBones.LeftLowerLeg;
            case HumanBodyBones.LeftLowerLeg:
                return HumanBodyBones.LeftFoot;
            case HumanBodyBones.RightUpperLeg:
                return HumanBodyBones.RightLowerLeg;
            case HumanBodyBones.RightLowerLeg:
                return HumanBodyBones.RightFoot;
            default:
                return parent;
        }
    }
}

//global player data, used to simplify information access between scripts
public static class PlayerInfo
{
    #region Initialization Events
    public static UnityEvent<LucidVismodel> OnAssignVismodel = new UnityEvent<LucidVismodel>();
    public static UnityEvent OnAnimModellInitialized = new UnityEvent();
    public static UnityEvent OnRemoveVismodel = new UnityEvent();
    #endregion

    #region References
    public static Transform
        pelvis,
        hips,
        head,
        hipspace,
        footspace,
        legspaceR,
        legspaceL,
        IK_RH,
        IK_LH,
        IK_RF,
        IK_LF,
        FPTransform;

    public static Rigidbody
        mainBody,
        physBodyRB,
        physHeadRB;

    public static BoxCollider physBody;
    public static SphereCollider physHead;
    public static Camera mainCamera;
    public static Animator playermodelAnim;
    public static LucidLegs legRef;
    public static LucidVismodel vismodelRef;
    #endregion

    #region Motion Data
    public static Vector3 
        footsurface, 
        footsurfL, 
        footsurfR, 
        climbrelative, 
        currentpush, 
        currentslide 
        = Vector3.zero;

    public static float traction = 1;

    public static float
        stepphase,
        animphase,
        legdiffL,
        legdiffR,
        airtime,
        alignment,
        movespeed,
        grounddist,
        slidesurfangle,
        slidepushanglethreshold
        = 0;

    public static bool 
        grounded,
        pelviscollision,
        physCollision,
        crawling,
        flying,
        grabL,
        grabR,
        forceIK_RH,
        forceIK_LH,
        climbing,
        validgrabL,
        validgrabR,
        headlocked,
        animModelInitialized
        = false;
    #endregion
}