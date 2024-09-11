using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class WorldInfo : MonoBehaviour
{
    [SerializeField] LayerMask geometryMaskLayers;
    private void Awake()
    {
        Shortcuts.geometryMask = geometryMaskLayers;
    }
}

public static class Shortcuts
{
    public static int geometryMask = 0;
    public static List<HumanBodyBones> hb2list = new List<HumanBodyBones> {
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
    };
    public static Quaternion QShortestRotation(Quaternion a, Quaternion b)
    {
        if (Quaternion.Dot(a, b) < 0)
        {
            return a * Quaternion.Inverse(QMultiply(b, -1));
        }
        else return a * Quaternion.Inverse(b);
    }

    public static Quaternion QMultiply(Quaternion input, float scalar)
    {
        return new Quaternion(input.x * scalar, input.y * scalar, input.z * scalar, input.w * scalar);
    }

}

//mostly used for interaction between motion-related scripts such as legs and modelsync
public static class PlayerInfo
{
    public static UnityEvent<LucidVismodel> OnAssignVismodel = new UnityEvent<LucidVismodel>();

    //references
    public static Rigidbody mainBody;
    public static Transform pelvis;
    public static Transform hips;
    public static Transform head;
    public static Transform hipspace;
    public static Transform footspace;
    public static Transform legspaceR;
    public static Transform legspaceL;
    public static Transform climbtargetL;
    public static Transform climbtargetR;
    public static Transform FPTransform;
    public static Collider physHips;
    public static Rigidbody physHipsRB;
    public static Collider physHead;
    public static Rigidbody physHeadRB;
    public static Camera mainCamera;
    public static Animator playermodelAnim;
    public static LucidLegs legRef;
    public static LucidVismodel vismodelRef;

    //motion data
    public static Vector3 footTargetL = Vector3.zero;
    public static Vector3 footTargetR = Vector3.zero;
    public static Vector3 handTargetL = Vector3.zero;
    public static Vector3 handTargetR = Vector3.zero;
    public static Vector3 footsurface = Vector3.zero;
    public static Vector3 footsurfL = Vector3.zero;
    public static Vector3 footsurfR = Vector3.zero;
    public static Vector3 climbrelative = Vector3.zero;
    public static Vector3 currentpush = Vector3.zero;
    public static Vector3 currentslide = Vector3.zero;
    public static float stepphase = 0;
    public static float animphase = 0;
    public static float legdiffL = 0;
    public static float legdiffR = 0;
    public static float traction = 1;
    public static float airtime = 0;
    public static float alignment = 0;
    public static bool grounded = false;
    public static bool pelviscollision = false;
    public static bool physCollision = false;
    public static bool crawling = false;
    public static bool flying = false;
    public static bool grabL = false;
    public static bool grabR = false;
    public static bool climbing = false;
    public static bool validgrabL = false;
    public static bool validgrabR = false;
    public static bool headlocked = true;
}