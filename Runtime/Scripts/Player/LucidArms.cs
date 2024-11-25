using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LucidArms : MonoBehaviour
{
    public Transform itemPosesR;
    public Transform itemPosesL;
    //poses: 0=one handed carry, 1=two handed carry

    [SerializeField] float
        shoulderdist,
        castdist,
        limitdist,
        animArmLengthMult,
        shoulderstiffness,
        pushpullstrength,
        firstcastwidth,
        secondcastwidth,
        runmaxY,
        minflatgrab,
        tolerance,
        lateralStrength,
        lateralMechanicalAdvantage,
        ungrabBoost,
        grabspring,
        grabdamp;

    [SerializeField] Transform leftunrotate, rightunrotate, handTargetL, handTargetR;
    [SerializeField] ConfigurableJoint jointReference;
    [SerializeField] Rigidbody staticGrabRB_L, staticGrabRB_R;
    private JointDrive jdvance;
    private SoftJointLimit sjlewis;
    private ConfigurableJoint leftAnchor, rightAnchor;
    private Rigidbody grabbedRB_R, grabbedRB_L;
    private Transform animShoulderL, animShoulderR;
    private Transform targetTransformL, targetTransformR;
    private Transform currentPoseL, currentPoseR;
    private Vector3 grabPositionL, grabPositionR;
    private Quaternion grabRotationL, grabRotationR;
    private Quaternion dynamicGrabRotationOffsetL, dynamicGrabRotationOffsetR;
    private bool 
        grabL,
        grabR,
        grabwaitL,
        grabwaitR,
        disabling,
        initialized,
        climbMode
        = false;
    private float hipdrag, headdrag = 0;
    private float animArmLength = 0;

    private void OnEnable()
    {
        if (LucidInputActionRefs.grabL != null)
            ManageInputSubscriptions(true);
        PlayerInfo.OnAssignVismodel.AddListener(OnAssignVismodel);
        PlayerInfo.handTargetL = handTargetL;
        PlayerInfo.handTargetR = handTargetR;
    }

    private void OnDisable()
    {
        disabling = true;
        grabwaitL = false;
        grabwaitR = false;
        Ungrab(false);
        Ungrab(true);
        PlayerInfo.climbing = false;

        ManageInputSubscriptions(false);
        PlayerInfo.OnAssignVismodel.RemoveListener(OnAssignVismodel);
    }

    private void ManageInputSubscriptions(bool subscribe)
    {
        if (subscribe)
        {
            LucidInputActionRefs.grabL.started += GrabButtonLeft;
            LucidInputActionRefs.grabL.canceled += UngrabButtonLeft;
            LucidInputActionRefs.grabR.started += GrabButtonRight;
            LucidInputActionRefs.grabR.canceled += UngrabButtonRight;
            LucidInputActionRefs.toggleClimbMode.started += ToggleClimbModeButton;
        }
        else
        {
            LucidInputActionRefs.grabL.started -= GrabButtonLeft;
            LucidInputActionRefs.grabL.canceled -= UngrabButtonLeft;
            LucidInputActionRefs.grabR.started -= GrabButtonRight;
            LucidInputActionRefs.grabR.canceled -= UngrabButtonRight;
            LucidInputActionRefs.toggleClimbMode.started -= ToggleClimbModeButton;
        }
    }

    private void OnAssignVismodel(LucidVismodel visModel)
    {
        disabling = false;
        animShoulderL = visModel.anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        animShoulderR = visModel.anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        hipdrag = PlayerInfo.physBodyRB.angularDrag;
        headdrag = PlayerInfo.physHeadRB.angularDrag;
        animArmLength = CalculateAnimArmLength(visModel);
        currentPoseL = itemPosesL.Find("OneHandedCarry");
        currentPoseR = itemPosesR.Find("OneHandedCarry");

        initialized = true;
    }

    private void Start()
    {
        ManageInputSubscriptions(true);
        InitializeJointProfiles();
    }

    //in short: uses an initial cast to find the nearest "wall", then another cast to find the top of said wall. If all is good and within reach, then we start asking if we're trying to grab and if so we call those functions up
    private void FixedUpdate()
    {
        if (disabling || animShoulderL == null || animShoulderR == null || !initialized) return;

        GrabLogic(false);
        GrabLogic(true);

        PlayerInfo.grabL = grabL;
        PlayerInfo.grabR = grabR;

        bool dynamicGrabL = (grabbedRB_L != null && grabbedRB_L != staticGrabRB_L);
        bool dynamicGrabR = (grabbedRB_R != null && grabbedRB_R != staticGrabRB_R);

        bool itemMode = (dynamicGrabL || dynamicGrabR) && !climbMode;

        PlayerInfo.climbing = (grabL || grabR) && !itemMode;

        CalcClimbRelative();

        Vector3 targetposL = PlayerInfo.pelvis.transform.InverseTransformPoint(animShoulderL.position);
        Vector3 targetposR = PlayerInfo.pelvis.transform.InverseTransformPoint(animShoulderR.position);

        if (leftAnchor != null)
            leftAnchor.anchor += (targetposL - leftAnchor.anchor) * shoulderstiffness;
        if (rightAnchor != null)
            rightAnchor.anchor += (targetposR - rightAnchor.anchor) * shoulderstiffness;

        if (PlayerInfo.climbing)
            ClimbPose();
        else
        {
            if (grabR)
                ItemPose(true);
            if (grabL)
                ItemPose(false);
        }
        if (grabL || grabR)
            HandPush();

        if (grabR)
        {
            Quaternion effectiveRotationR = grabRotationR;
            if (dynamicGrabR)
            {
                grabPositionR = grabbedRB_R.transform.TransformPoint(rightAnchor.connectedAnchor);
                effectiveRotationR = grabbedRB_R.rotation * dynamicGrabRotationOffsetR;
            }
            PlayerInfo.IK_RH.position = grabPositionR;
            PlayerInfo.IK_RH.rotation = effectiveRotationR;
        }
        if (grabL)
        {
            Quaternion effectiveRotationL = grabRotationL;
            if (dynamicGrabL)
            {
                grabPositionL = grabbedRB_L.transform.TransformPoint(leftAnchor.connectedAnchor);
                effectiveRotationL = grabbedRB_L.rotation * dynamicGrabRotationOffsetL;
            }
            PlayerInfo.IK_LH.position = grabPositionL;
            PlayerInfo.IK_LH.rotation = effectiveRotationL;
        }
    }

    private void InitializeJointProfiles()
    {
        jdvance.positionSpring = grabspring;
        jdvance.positionDamper = grabdamp;

        sjlewis.limit = limitdist;
    }

    private void UpdateItemPose(string pose)
    {
        Transform poseL = itemPosesL.Find(pose);
        Transform poseR = itemPosesR.Find(pose);
        if (poseL == null)
            poseL = itemPosesL.GetChild(0);
        if (poseR == null)
            poseR = itemPosesR.GetChild(0);
        currentPoseL = poseL;
        currentPoseR = poseR;
    }

    private void ItemPose(bool isRight)
    {
        ref Transform itemPoses = ref itemPosesL;
        if (isRight)
            itemPoses = ref itemPosesR;

        ref Transform handTarget = ref handTargetL;
        if (isRight)
            handTarget = ref handTargetR;

        Transform currentPose = isRight ? currentPoseR : currentPoseL;

        Transform animShoulder = isRight ? animShoulderR : animShoulderL;

        itemPoses.position = animShoulder.position;
        itemPoses.rotation = PlayerInfo.pelvis.rotation;
        Vector3 pos = itemPoses.TransformPoint(currentPose.localPosition * animArmLength);
        handTarget.position = pos;
        handTarget.rotation = currentPose.localRotation * PlayerInfo.head.rotation;
    }

    private void ClimbPose()
    {
        float pull = (LucidInputValueShortcuts.crouch ? 1 : 0);

        Vector2 inputmove = LucidInputValueShortcuts.movement;
        Vector3 moveflat = Vector3.zero;
        moveflat.x = inputmove.x;
        moveflat.z = inputmove.y;
        Vector3 motion = PlayerInfo.pelvis.TransformVector(moveflat);
        motion.y += pull;
        motion *= animArmLength;

        handTargetL.transform.position = grabPositionL - motion;
        handTargetR.transform.position = grabPositionR - motion;
    }

    private void HandPush()
    {
        if (leftAnchor != null)
        {
            leftAnchor.targetPosition = leftAnchor.transform.InverseTransformPoint(handTargetL.position) - leftAnchor.anchor;
            leftAnchor.targetRotation = handTargetL.rotation * Quaternion.Inverse(leftAnchor.transform.rotation);
        }
        if (rightAnchor != null)
        {
            rightAnchor.targetPosition = rightAnchor.transform.InverseTransformPoint(handTargetR.position) - rightAnchor.anchor;
            rightAnchor.targetRotation = handTargetR.rotation * Quaternion.Inverse(rightAnchor.transform.rotation);
        }
    }

    private float CalculateAnimArmLength(LucidVismodel visModel)
    {
        Vector3 shoulderPos = visModel.anim.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
        Vector3 elbowPos = visModel.anim.GetBoneTransform(HumanBodyBones.RightLowerArm).position;
        Vector3 wristPos = visModel.anim.GetBoneTransform(HumanBodyBones.RightHand).position;

        float upperArmLength = Vector3.Distance(shoulderPos, elbowPos);
        float lowerArmLength = Vector3.Distance(elbowPos, wristPos);

        return upperArmLength + lowerArmLength;
    }

    private void GrabLogic(bool isRight)
    {
        bool grabToCheck = (isRight ? grabR : grabL);

        ref Vector3 grabPosition = ref grabPositionL;
        if (isRight)
            grabPosition = ref grabPositionR;

        ref Quaternion grabRotation = ref grabRotationL;
        if (isRight)
            grabRotation = ref grabRotationR;

        ref Rigidbody staticRB = ref staticGrabRB_L;
        if (isRight)
            staticRB = ref staticGrabRB_R;

        ref Transform targetTransform = ref targetTransformL;
        if (isRight)
            targetTransform = ref targetTransformR;

        ConfigurableJoint targetanchor = isRight ? rightAnchor : leftAnchor;

        if (!grabToCheck)
        {

            bool validgrab = ClimbScan(isRight, out Vector3 position, out Quaternion rotation, out Transform hitTransform);

            if (validgrab)
            {
                grabPosition = position;
                grabRotation = rotation;
                targetTransform = hitTransform;
            }

            bool oldgrab = isRight ? PlayerInfo.validgrabR : PlayerInfo.validgrabL;
            if (isRight)
                PlayerInfo.validgrabR = validgrab;
            else
                PlayerInfo.validgrabL = validgrab;

            bool grabwait = (isRight ? grabwaitR : grabwaitL);
            if (!oldgrab && validgrab && grabwait)
            {
                Grab(isRight);
            }

            staticRB.transform.position = grabPosition;
            staticRB.transform.rotation = grabRotation;
        }
        else
        {
            if (isRight)
                PlayerInfo.validgrabR = true;
            else
                PlayerInfo.validgrabL = true;
        }
    }

    private bool ClimbScan(bool right, out Vector3 position, out Quaternion rotation, out Transform targetTransform)
    {
        Transform cam = PlayerInfo.head;
        Vector3 campos = cam.position;
        Vector3 camfwd = cam.forward;
        Vector3 camright = cam.right;
        bool vaulting = (grabwaitR ^ grabwaitL) && !(grabL || grabR);
        if (vaulting)
        {
            campos += cam.forward;
            camfwd *= -1;
        }
        float castAdjust = animArmLength + castdist;

        Vector3 shoulder = campos + (right ? camright * shoulderdist : -camright * shoulderdist);

        bool initialHit = Physics.SphereCast(shoulder, firstcastwidth, camfwd, out RaycastHit initialHitInfo, castAdjust, Shortcuts.geometryMask);
        initialHit &= ((initialHitInfo.normal.y) < runmaxY);

        if (initialHit)
        {
            if (initialHitInfo.transform.gameObject.CompareTag("Grabbable"))
            {
                Vector3 jointfwd = PlayerInfo.pelvis.forward;
                if (Mathf.Abs(initialHitInfo.normal.y) < 0.05f)
                    jointfwd = Vector3.up;

                targetTransform = initialHitInfo.transform;
                position = initialHitInfo.point;
                rotation = Quaternion.LookRotation(jointfwd, initialHitInfo.normal);

                return true;
            }
        }


        Vector3 projectvector = Vector3.up;
        if (initialHitInfo.normal.y < -0.05f)
            projectvector = -PlayerInfo.pelvis.forward + Vector3.up;
        else if (initialHitInfo.normal.y > 0.05f)
            projectvector = PlayerInfo.pelvis.forward + Vector3.up;

        Vector3 hitvector = Vector3.ProjectOnPlane(projectvector, initialHitInfo.normal).normalized;
        if (Mathf.Abs(initialHitInfo.normal.y) < 0.01f)
            hitvector = Vector3.up;

        float angle = Vector3.Angle(-cam.forward, hitvector);
        float dist = Vector3.Distance(shoulder, initialHitInfo.point);

        //i know this trig stuff looks scary but that's just how it knows where to look when you're dealing with a sloped wall
        float sinC = (dist * Mathf.Sin(Mathf.Deg2Rad * angle)) / castAdjust;
        float A = 180 - (Mathf.Asin(sinC) + angle);
        float newmaxheight = (Mathf.Sin(Mathf.Deg2Rad * A) * castAdjust) / Mathf.Sin(Mathf.Deg2Rad * angle);

        Vector3 startpoint = initialHitInfo.point + (hitvector.normalized * newmaxheight);

        bool surfaceCastHit = Physics.SphereCast(startpoint - initialHitInfo.normal * secondcastwidth, secondcastwidth, -hitvector, out RaycastHit downcastHitInfo, newmaxheight, Shortcuts.geometryMask);
        bool validgrab = initialHit && surfaceCastHit && downcastHitInfo.normal.y > minflatgrab;

        if (validgrab)
        {
            position = downcastHitInfo.point;
            Vector3 transformhitinfo = Vector3.ProjectOnPlane(-initialHitInfo.normal, downcastHitInfo.normal);
            rotation = Quaternion.LookRotation(transformhitinfo, downcastHitInfo.normal);
        }
        else
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
        }

        targetTransform = downcastHitInfo.transform;

        return validgrab;
    }

    private void CalcClimbRelative()
    {
        Vector3 center = Vector3.zero;
        if (grabR)
        {
            center += rightunrotate.transform.InverseTransformPoint(PlayerInfo.pelvis.position);
        }
        if (grabL)
        {
            center += leftunrotate.transform.InverseTransformPoint(PlayerInfo.pelvis.position);
        }
        if (grabL && grabR)
        {
            center /= 2;
        }

        Vector3 climbrelative = center;
        climbrelative.z *= 4;
        climbrelative.y *= 4;
        PlayerInfo.climbrelative = climbrelative;
    }


    #region Input Events
    private void GrabButtonLeft(InputAction.CallbackContext obj)
    {
        if (PlayerInfo.validgrabL)
            Grab(false);
        else
            grabwaitL = true;
    }

    private void GrabButtonRight(InputAction.CallbackContext obj)
    {
        if (PlayerInfo.validgrabR)
            Grab(true);
        else
            grabwaitR = true;
    }

    private void UngrabButtonLeft(InputAction.CallbackContext obj)
    {
        grabwaitL = false;
        Ungrab(false);
    }

    private void UngrabButtonRight(InputAction.CallbackContext obj)
    {
        grabwaitR = false;
        Ungrab(true);
    }

    private void ToggleClimbModeButton(InputAction.CallbackContext obj)
    {
        climbMode = !climbMode;
    }
    #endregion

    private void Grab(bool isRight)
    {
        if (!initialized) return;

        ref Vector3 grabPosition = ref grabPositionL;
        if (isRight)
            grabPosition = ref grabPositionR;

        ref Quaternion grabRotation = ref grabRotationL;
        if (isRight)
            grabRotation = ref grabRotationR;

        Transform targetTransform = isRight ? targetTransformR : targetTransformL;

        CreateConfigurableJoint(isRight, grabPosition, grabRotation, targetTransform);

        ref bool grab = ref grabL;
        if (isRight)
            grab = ref grabR;

        ref bool grabwait = ref grabwaitL;
        if (isRight)
            grabwait = ref grabwaitR;

        GrabTrigger trig = targetTransform.GetComponent<GrabTrigger>();
        if (trig != null)
            trig.GrabEvent();

        if (grabL && grabR)
            UpdateItemPose("TwoHandedCarry");
        else
            UpdateItemPose("OneHandedCarry");

        grabwait = false;
        grab = true;
    }

    private void Ungrab(bool isRight)
    {
        ref bool grab = ref grabL;
        if (isRight)
            grab = ref grabR;

        if (!initialized || !grab) return;

        Transform targetTransform = isRight ? targetTransformR : targetTransformL;

        ref Rigidbody grabbedRB = ref grabbedRB_L;
        if (isRight)
            grabbedRB = ref grabbedRB_R;

        ref ConfigurableJoint jointTarget = ref leftAnchor;
        if (isRight)
            jointTarget = ref rightAnchor;

        Destroy(jointTarget);

        GrabTrigger trig = targetTransform.GetComponent<GrabTrigger>();
        if (trig != null)
            trig.UngrabEvent();
        grabbedRB = null;

        Vector3 dir = Vector3.ClampMagnitude(PlayerInfo.mainBody.velocity, 1);
        PlayerInfo.mainBody.AddForce(dir * ungrabBoost, ForceMode.Acceleration);
        PlayerInfo.physBodyRB.angularDrag = hipdrag;
        PlayerInfo.physHeadRB.angularDrag = headdrag;

        UpdateItemPose("OneHandedCarry");

        grab = false;
    }

    private void CreateConfigurableJoint(bool isRight, Vector3 grabPosition, Quaternion grabRotation, Transform grabTarget)
    {
        ref Quaternion dynamicGrabRotationOffset = ref dynamicGrabRotationOffsetL;
        if (isRight)
            dynamicGrabRotationOffset = ref dynamicGrabRotationOffsetR;

        ref ConfigurableJoint jointTarget = ref leftAnchor;
        if (isRight)
            jointTarget = ref rightAnchor;

        ref Rigidbody grabbedRB = ref grabbedRB_L;
        if (isRight)
            grabbedRB = ref grabbedRB_R;

        ref Rigidbody staticRB = ref staticGrabRB_L;
        if (isRight)
            staticRB = ref staticGrabRB_R;

        if (jointTarget != null)
            Destroy(jointTarget);

        bool dynamic = false;

        Rigidbody grabTargetRB = grabTarget.GetComponent<Rigidbody>();
        if (grabTargetRB != null)
        {
            dynamic = true;
            grabbedRB = grabTargetRB;
            dynamicGrabRotationOffset = grabRotation * Quaternion.Inverse(grabbedRB.rotation);
        }
        else
            grabbedRB = staticRB;

        jointTarget = Shortcuts.AddComponent(PlayerInfo.pelvis.gameObject, jointReference);
        jointTarget.autoConfigureConnectedAnchor = false;
        jointTarget.linearLimit = sjlewis;
        jointTarget.connectedBody = grabbedRB;
        if (dynamic)
            jointTarget.connectedAnchor = grabTarget.InverseTransformPoint(grabPosition);
    }
}
