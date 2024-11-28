using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LucidArms : MonoBehaviour
{
    public Transform itemPosesR;
    public Transform itemPosesL;
    //poses: 0=one handed carry, 1=two handed carry

    [SerializeField]
    float
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
        grabdamp,
        climbForceThreshold;

    [SerializeField] Transform unrotateL, unrotateR, handTargetL, handTargetR;
    [SerializeField] ConfigurableJoint jointReference;
    [SerializeField] Rigidbody staticGrabRB_L, staticGrabRB_R;
    [SerializeField] LayerMask CastMask;
    private JointDrive jdvance;
    private SoftJointLimit sjlewis;
    private EventBox eventBoxL, eventBoxR;
    private ConfigurableJoint anchorL, anchorR;
    private Rigidbody grabbedRB_R, grabbedRB_L;
    private LucidTool lt_R, lt_L;
    private Transform animShoulderL, animShoulderR;
    private Transform targetTransformL, targetTransformR;
    private Transform currentPoseL, currentPoseR;
    private Vector3 grabPositionL, grabPositionR;
    private Vector3 grabForceL, grabForceR;
    private Quaternion grabRotationL, grabRotationR;
    private Quaternion dynamicGrabRotationOffsetL, dynamicGrabRotationOffsetR;
    private Quaternion poseOffsetL, poseOffsetR;
    private bool
        grabL,
        grabR,
        grabwaitL,
        grabwaitR,
        grabLockL,
        grabLockR,
        isPrimaryL,
        isPrimaryR,
        disabling,
        initialized
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
        PlayerInfo.climbL = false;
        PlayerInfo.climbR = false;
        PlayerInfo.climbing = false;

        ManageInputSubscriptions(false);
        PlayerInfo.OnAssignVismodel.RemoveListener(OnAssignVismodel);
    }

    private void ManageInputSubscriptions(bool subscribe)
    {
        if (subscribe)
        {
            LucidInputActionRefs.grabL.started += GrabButtonLeftDown;
            LucidInputActionRefs.grabL.canceled += GrabButtonLeftUp;
            LucidInputActionRefs.grabR.started += GrabButtonRightDown;
            LucidInputActionRefs.grabR.canceled += GrabButtonRightUp;
            LucidInputActionRefs.dropL.started += DropButtonL;
            LucidInputActionRefs.dropR.started += DropButtonR;
        }
        else
        {
            LucidInputActionRefs.grabL.started -= GrabButtonLeftDown;
            LucidInputActionRefs.grabL.canceled -= GrabButtonLeftUp;
            LucidInputActionRefs.grabR.started -= GrabButtonRightDown;
            LucidInputActionRefs.grabR.canceled -= GrabButtonRightUp;
            LucidInputActionRefs.dropL.started -= DropButtonL;
            LucidInputActionRefs.dropR.started -= DropButtonR;
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

        Vector3 targetposL = PlayerInfo.pelvis.transform.InverseTransformPoint(animShoulderL.position);
        Vector3 targetposR = PlayerInfo.pelvis.transform.InverseTransformPoint(animShoulderR.position);

        if (anchorL != null)
        {
            anchorL.anchor += (targetposL - anchorL.anchor) * shoulderstiffness;
            grabForceL = anchorL.currentForce;
        }
        else
            grabForceL = Vector3.down * 1000;
        if (anchorR != null)
        {
            anchorR.anchor += (targetposR - anchorR.anchor) * shoulderstiffness;
            grabForceR = anchorR.currentForce;
        }
        else
            grabForceR = Vector3.down * 1000;

        PlayerInfo.grabL = grabL;
        PlayerInfo.grabR = grabR;

        bool dynamicGrabL = (grabbedRB_L != null && grabbedRB_L != staticGrabRB_L);
        bool dynamicGrabR = (grabbedRB_R != null && grabbedRB_R != staticGrabRB_R);

        float threshold = climbForceThreshold;
        if (grabL && grabR)
            threshold /= 2;

        if ((grabForceL.y > threshold || !dynamicGrabL) && grabL)
            PlayerInfo.climbL = true;
        if ((grabForceR.y > threshold || !dynamicGrabR) && grabR)
            PlayerInfo.climbR = true;

        if (PlayerInfo.climbL || PlayerInfo.climbR)
            PlayerInfo.climbing = true;

        CalcClimbRelative();

        if (PlayerInfo.climbL) 
            ClimbPose(false);
        else 
            ItemPose(false);

        if (PlayerInfo.climbR) 
            ClimbPose(true);
        else 
            ItemPose(true);

        if (grabL || grabR)
            HandPush();

        if (grabR)
        {
            Quaternion effectiveRotationR = grabRotationR;
            if (dynamicGrabR)
            {
                grabPositionR = grabbedRB_R.transform.TransformPoint(anchorR.connectedAnchor);
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
                grabPositionL = grabbedRB_L.transform.TransformPoint(anchorL.connectedAnchor);
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

    private void UpdateItemPose(bool isRight, string pose)
    {
        Transform tPoseParent = isRight ? itemPosesR : itemPosesL;
        Transform tPose = tPoseParent.Find(pose);
        ref LucidTool lt = ref lt_L;
        if (isRight)
            lt = ref lt_R;
        if (lt != null)
        {
            bool doPrimary = isRight ? isPrimaryR : isPrimaryL;
            if (isRight)
                tPose = doPrimary ? lt.itemPosePrimaryR : lt.itemPoseSecondaryR;
            else
                tPose = doPrimary ? lt.itemPosePrimaryL : lt.itemPoseSecondaryL;
        }
        if (tPose == null)
            tPose = tPoseParent.GetChild(0);
        ref Transform currentPose = ref currentPoseL;
        if (isRight)
            currentPose = ref currentPoseR;
        currentPose = tPose;
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
        handTarget.rotation = PlayerInfo.pelvis.rotation * currentPose.localRotation;
    }

    private void ClimbPose(bool isRight)
    {
        float pull = (LucidInputValueShortcuts.crouch ? 1 : 0);

        Vector2 inputmove = LucidInputValueShortcuts.movement;
        Vector3 moveflat = Vector3.zero;
        moveflat.x = inputmove.x;
        moveflat.z = inputmove.y;
        Vector3 motion = PlayerInfo.pelvis.TransformVector(moveflat);
        motion.y += pull;
        motion *= animArmLength;

        if (isRight)
            handTargetR.transform.position = grabPositionR - motion;
        else
            handTargetL.transform.position = grabPositionL - motion;
    }

    private void HandPush()
    {
        bool dynamicGrabL = (grabbedRB_L != null && grabbedRB_L != staticGrabRB_L);
        bool dynamicGrabR = (grabbedRB_R != null && grabbedRB_R != staticGrabRB_R);
        if (anchorL != null)
        {
            anchorL.targetPosition = anchorL.transform.InverseTransformPoint(handTargetL.position) - anchorL.anchor;
            Quaternion localhand = Quaternion.Inverse(PlayerInfo.pelvis.rotation) * handTargetL.rotation;
            localhand *= Quaternion.Inverse(dynamicGrabRotationOffsetL);
            if (dynamicGrabL)
                anchorL.targetRotation = localhand * poseOffsetL;
            else
                anchorL.targetRotation = Quaternion.identity;
        }
        if (anchorR != null)
        {
            Quaternion localhand = Quaternion.Inverse(PlayerInfo.pelvis.rotation) * handTargetR.rotation;
            localhand *= Quaternion.Inverse(dynamicGrabRotationOffsetR);
            anchorR.targetPosition = anchorR.transform.InverseTransformPoint(handTargetR.position) - anchorR.anchor;
            if (dynamicGrabR)
                anchorR.targetRotation = localhand * poseOffsetR;
            else
                anchorR.targetRotation = Quaternion.identity;
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

        ConfigurableJoint targetanchor = isRight ? anchorR : anchorL;

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

        bool initialHit = Physics.SphereCast(shoulder, firstcastwidth, camfwd, out RaycastHit initialHitInfo, castAdjust, CastMask);
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
                Vector3 transformhitinfo = Vector3.ProjectOnPlane(jointfwd, initialHitInfo.normal);
                rotation = Quaternion.LookRotation(transformhitinfo, initialHitInfo.normal);

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

        bool surfaceCastHit = Physics.SphereCast(startpoint - initialHitInfo.normal * secondcastwidth, secondcastwidth, -hitvector, out RaycastHit downcastHitInfo, newmaxheight, CastMask);
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
        if (PlayerInfo.climbR)
        {
            center += unrotateR.transform.InverseTransformPoint(PlayerInfo.pelvis.position);
        }
        if (PlayerInfo.climbL)
        {
            center += unrotateL.transform.InverseTransformPoint(PlayerInfo.pelvis.position);
        }
        if (PlayerInfo.climbL && PlayerInfo.climbR)
        {
            center /= 2;
        }

        Vector3 climbrelative = center;
        climbrelative.z *= 4;
        climbrelative.y *= 4;
        PlayerInfo.climbrelative = climbrelative;
    }


    #region Input Events
    private void GrabButtonLeftDown(InputAction.CallbackContext obj)
    {
        if (lt_L != null && grabLockL)
            lt_L.Use.Invoke();
        else
        {
            if (PlayerInfo.validgrabL)
                Grab(false);
            else
                grabwaitL = true;
        }
    }

    private void GrabButtonRightDown(InputAction.CallbackContext obj)
    {
        if (lt_R != null && grabLockR)
            lt_R.Use.Invoke();
        else
        {
            if (PlayerInfo.validgrabR)
                Grab(true);
            else
                grabwaitR = true;
        }
    }

    private void GrabButtonLeftUp(InputAction.CallbackContext obj)
    {
        grabwaitL = false;
        if (!grabLockL)
            Ungrab(false);
    }

    private void GrabButtonRightUp(InputAction.CallbackContext obj)
    {
        grabwaitR = false;
        if (!grabLockR)
            Ungrab(true);
    }

    private void DropButtonL(InputAction.CallbackContext obj)
    {
        if (grabL && grabLockL)
        {
            grabwaitL = false;
            Ungrab(false);
        }
    }

    private void DropButtonR(InputAction.CallbackContext obj)
    {
        if (grabR && grabLockR)
        {
            grabwaitR = false;
            Ungrab(true);
        }
    }
    #endregion

    private void Grab(bool isRight)
    {
        bool grablock = isRight ? grabLockR : grabLockL;
        if (!initialized || grablock) return;

        ref Vector3 grabPosition = ref grabPositionL;
        if (isRight)
            grabPosition = ref grabPositionR;

        ref Quaternion grabRotation = ref grabRotationL;
        if (isRight)
            grabRotation = ref grabRotationR;

        ref bool grabLock = ref grabLockL;
        if (isRight)
            grabLock = ref grabLockR;

        ref bool isPrimary = ref isPrimaryL;
        if (isRight)
            isPrimary = ref isPrimaryR;

        ref LucidTool lt = ref lt_L;
        if (isRight)
            lt = ref lt_R;

        LucidTool otherLT = isRight ? lt_L : lt_R;

        Transform targetTransform = isRight ? targetTransformR : targetTransformL;

        lt = targetTransform.GetComponent<LucidTool>();
        if (lt != null)
        {
            isPrimary = (lt != otherLT);
            Transform targetGrip = isPrimary ? lt.PrimaryGripL : lt.SecondaryGripL;
            if (isRight)
                targetGrip = isPrimary ? lt.PrimaryGripR : lt.SecondaryGripR;
            grabPosition = targetGrip.position;
            grabRotation = targetGrip.rotation;
            grabLock = isPrimary ? lt.grabLockPrimary : lt.grabLockSecondary;
        }

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
        {
            UpdateItemPose(false, "TwoHandedCarry");
            UpdateItemPose(true, "TwoHandedCarry");
        }
        else
            UpdateItemPose(isRight, "OneHandedCarry");

        grabwait = false;
        grab = true;
    }

    private void Ungrab(bool isRight)
    {
        ref bool grab = ref grabL;
        if (isRight)
            grab = ref grabR;

        ref bool climb = ref PlayerInfo.climbL;
        if (isRight)
            climb = ref PlayerInfo.climbR;

        ref bool otherClimb = ref PlayerInfo.climbL;
        if (isRight)
            otherClimb = ref PlayerInfo.climbR;

        ref bool grabLock = ref grabLockL;
        if (isRight)
            grabLock = ref grabLockR;

        if (!initialized || !grab) return;

        Transform targetTransform = isRight ? targetTransformR : targetTransformL;

        ref Rigidbody grabbedRB = ref grabbedRB_L;
        if (isRight)
            grabbedRB = ref grabbedRB_R;

        ref ConfigurableJoint jointTarget = ref anchorL;
        if (isRight)
            jointTarget = ref anchorR;

        ref LucidTool lt = ref lt_L;
        if (isRight)
            lt = ref lt_R;

        LucidTool otherLT = isRight ? lt_L : lt_R;

        Destroy(jointTarget);

        GrabTrigger trig = targetTransform.GetComponent<GrabTrigger>();
        if (trig != null)
            trig.UngrabEvent();
        grabbedRB = null;

        Vector3 dir = Vector3.ClampMagnitude(PlayerInfo.mainBody.velocity, 1);
        PlayerInfo.mainBody.AddForce(dir * ungrabBoost, ForceMode.Acceleration);
        PlayerInfo.physBodyRB.angularDrag = hipdrag;
        PlayerInfo.physHeadRB.angularDrag = headdrag;

        UpdateItemPose(!isRight, "OneHandedCarry");

        grab = false;
        climb = false;
        lt = null;
        grabLock = false;

        Vector3 otherGrabForce = isRight ? grabForceL : grabForceR;

        if (otherGrabForce.y < climbForceThreshold)
            otherClimb = false;

        if (!otherClimb)
            PlayerInfo.climbing = false;
    }

    private void CreateConfigurableJoint(bool isRight, Vector3 grabPosition, Quaternion grabRotation, Transform grabTarget)
    {
        ref Quaternion dynamicGrabRotationOffset = ref dynamicGrabRotationOffsetL;
        if (isRight)
            dynamicGrabRotationOffset = ref dynamicGrabRotationOffsetR;

        ref Quaternion poseOffset = ref poseOffsetL;
        if (isRight)
            poseOffset = ref poseOffsetR;

        ref ConfigurableJoint jointTarget = ref anchorL;
        if (isRight)
            jointTarget = ref anchorR;

        ref Rigidbody grabbedRB = ref grabbedRB_L;
        if (isRight)
            grabbedRB = ref grabbedRB_R;

        ref Rigidbody staticRB = ref staticGrabRB_L;
        if (isRight)
            staticRB = ref staticGrabRB_R;

        ref EventBox eventBox = ref eventBoxL;
        if (isRight)
            eventBox = ref eventBoxR;

        if (jointTarget != null)
            Destroy(jointTarget);

        bool dynamic = false;

        Rigidbody grabTargetRB = grabTarget.GetComponent<Rigidbody>();
        if (grabTargetRB != null)
        {
            dynamic = true;
            grabbedRB = grabTargetRB;
            dynamicGrabRotationOffset = Quaternion.Inverse(grabbedRB.rotation) * grabRotation;
            poseOffset = Quaternion.Inverse(grabbedRB.rotation) * PlayerInfo.pelvis.rotation;

            eventBox = grabTarget.gameObject.AddComponent<EventBox>();
            eventBox.onCollisionExit = new UnityEngine.Events.UnityEvent<Collision>();
            if (isRight)
                eventBox.onCollisionExit.AddListener(CollisionExitCallbackR);
            else
                eventBox.onCollisionExit.AddListener(CollisionExitCallbackL);
        }
        else
        {
            grabbedRB = staticRB;
            poseOffset = Quaternion.identity;
            dynamicGrabRotationOffset = Quaternion.identity;
        }

        jointTarget = Shortcuts.AddComponent(PlayerInfo.pelvis.gameObject, jointReference);
        jointTarget.autoConfigureConnectedAnchor = false;
        jointTarget.linearLimit = sjlewis;
        jointTarget.connectedBody = grabbedRB;
        if (dynamic)
            jointTarget.connectedAnchor = grabTarget.InverseTransformPoint(grabPosition);
    }

    public void CollisionExitCallbackL(Collision c)
    {
        if (grabForceL.y < climbForceThreshold)
            PlayerInfo.climbL = false;
    }

    public void CollisionExitCallbackR(Collision c)
    {
        if (grabForceR.y > climbForceThreshold)
            PlayerInfo.climbR = false;
    }
}
