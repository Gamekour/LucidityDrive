using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LucidArms : MonoBehaviour
{
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
        ungrabBoost;

    [SerializeField] Rigidbody leftAnchorRB, rightAnchorRB;
    [SerializeField] ConfigurableJoint leftAnchor, rightAnchor;
    [SerializeField] Transform leftunrotate, rightunrotate, handTargetL, handTargetR;
    private FixedJoint leftAnchorFixed, rightAnchorFixed;
    private Rigidbody grabbedRB_R, grabbedRB_L;
    private Transform animShoulderL, animShoulderR;
    private Transform currenttargetL, currenttargetR;
    private SoftJointLimit off, on = new SoftJointLimit();
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
        leftAnchor.connectedBody = PlayerInfo.mainBody;
        rightAnchor.connectedBody = PlayerInfo.mainBody;
        hipdrag = PlayerInfo.physBodyRB.angularDrag;
        headdrag = PlayerInfo.physHeadRB.angularDrag;
        animArmLength = CalculateAnimArmLength(visModel);

        initialized = true;
    }

    private void Start()
    {
        ManageInputSubscriptions(true);
        off.limit = Mathf.Infinity;
        on.limit = animArmLength + limitdist;
        leftAnchor.linearLimit = off;
        rightAnchor.linearLimit = off;
    }

    //in short: uses an initial cast to find the nearest "wall", then another cast to find the top of said wall. If all is good and within reach, then we start asking if we're trying to grab and if so we call those functions up
    private void FixedUpdate()
    {
        if (disabling || animShoulderL == null || animShoulderR == null || !initialized) return;

        GrabLogic(false);
        GrabLogic(true);

        PlayerInfo.grabL = grabL;
        PlayerInfo.grabR = grabR;

        bool itemMode = (grabbedRB_L != null || grabbedRB_R != null) && !climbMode;

        PlayerInfo.climbing = (grabL || grabR) && !itemMode;

        CalcClimbRelative();

        Vector3 targetposL = PlayerInfo.pelvis.transform.InverseTransformPoint(animShoulderL.position);
        Vector3 targetposR = PlayerInfo.pelvis.transform.InverseTransformPoint(animShoulderR.position);

        leftAnchor.connectedAnchor += (targetposL - leftAnchor.connectedAnchor) * shoulderstiffness;
        rightAnchor.connectedAnchor += (targetposR - rightAnchor.connectedAnchor) * shoulderstiffness;

        if (PlayerInfo.climbing)
            HandPush();

        if (PlayerInfo.grabR)
        {
            PlayerInfo.IK_RH.position = rightAnchor.transform.position;
            PlayerInfo.IK_RH.rotation = rightAnchor.transform.rotation;
        }
        if (PlayerInfo.grabL)
        {
            PlayerInfo.IK_LH.position = leftAnchor.transform.position;
            PlayerInfo.IK_LH.rotation = leftAnchor.transform.rotation;
        }
    }

    private void HandPush()
    {
        float pull = (LucidInputValueShortcuts.crouch ? 1 : 0);

        Vector2 inputmove = LucidInputValueShortcuts.movement;
        Vector3 moveflat = Vector3.zero;
        moveflat.x = inputmove.x;
        moveflat.z = inputmove.y;
        Vector3 motion = PlayerInfo.pelvis.TransformVector(moveflat);
        motion.y += pull;
        motion *= animArmLength;

        handTargetL.transform.position = leftAnchor.transform.position - motion;
        handTargetR.transform.position = rightAnchor.transform.position - motion;

        Vector3 handoffsetL = handTargetL.transform.position - leftAnchor.transform.position;
        Vector3 outdirL = leftAnchor.transform.position - animShoulderL.position;
        outdirL.Normalize();
        if (outdirL.y < 0)
            outdirL = -outdirL;
        float dotL = Mathf.Abs(Vector3.Dot(handoffsetL.normalized, outdirL));
        Vector3 pushpull_L = -outdirL * dotL * handoffsetL.magnitude;
        Vector3 lateral_L = Vector3.ProjectOnPlane(handoffsetL, outdirL).normalized * (1 - dotL) * handoffsetL.magnitude;
        float armextensionL = Mathf.Clamp01(Vector3.Distance(leftAnchor.transform.position, animShoulderL.position));
        float mechanicalAdvantageL = ((1 - armextensionL) * lateralMechanicalAdvantage) + 1;

        Vector3 handactionL = (pushpull_L * pushpullstrength) + (lateral_L * lateralStrength * mechanicalAdvantageL);

        Vector3 handoffsetR = handTargetR.transform.position - rightAnchor.transform.position;
        Vector3 outdirR = rightAnchor.transform.position - animShoulderR.position;
        outdirR.Normalize();
        if (outdirR.y < 0)
            outdirR = -outdirR;
        float dotR = Mathf.Abs(Vector3.Dot(handoffsetR.normalized, outdirR));
        Vector3 pushpull_R = -outdirR * dotR * handoffsetR.magnitude;
        Vector3 lateral_R = Vector3.ProjectOnPlane(handoffsetR, outdirR).normalized * (1 - dotR) * handoffsetR.magnitude;
        float armextensionR = Mathf.Clamp01(Vector3.Distance(rightAnchor.transform.position, animShoulderR.position));
        float mechanicalAdvantageR = ((1 - armextensionR) * lateralMechanicalAdvantage) + 1;

        Vector3 handactionR = (pushpull_R * pushpullstrength) + (lateral_R * lateralStrength * mechanicalAdvantageR);

        PlayerInfo.mainBody.AddForce(-(handactionL + handactionR));

        if (grabbedRB_L != null)
            grabbedRB_L.AddForceAtPosition(handactionL, leftAnchor.transform.position);
        if (grabbedRB_R != null)
            grabbedRB_R.AddForceAtPosition(handactionR, rightAnchor.transform.position);
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
        ConfigurableJoint targetanchor = isRight ? rightAnchor : leftAnchor;

        if (!grabToCheck)
        {
            targetanchor.transform.position = PlayerInfo.pelvis.position;

            bool validgrab = ClimbScan(isRight);

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

            bool toofar = false;
            if (toofar) //ungrab if something pushes you far enough to "lose grip"
                Ungrab(isRight);
        }
        else
        {
            if (isRight)
                PlayerInfo.validgrabR = true;
            else
                PlayerInfo.validgrabL = true;
        }
    }

    private bool ClimbScan(bool right)
    {
        ConfigurableJoint jointTarget = right ? rightAnchor : leftAnchor;
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
                jointTarget.transform.position = initialHitInfo.point;

                Vector3 jointfwd = PlayerInfo.pelvis.forward;
                if (Mathf.Abs(initialHitInfo.normal.y) < 0.05f)
                    jointfwd = Vector3.up;
                jointTarget.transform.rotation = Quaternion.LookRotation(jointfwd, initialHitInfo.normal);

                if (right)
                    currenttargetR = initialHitInfo.transform;
                else
                    currenttargetL = initialHitInfo.transform;

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
            jointTarget.transform.position = downcastHitInfo.point;
            Vector3 transformhitinfo = Vector3.ProjectOnPlane(-initialHitInfo.normal, downcastHitInfo.normal);
            jointTarget.transform.rotation = Quaternion.LookRotation(transformhitinfo, downcastHitInfo.normal);
        }

        if (right)
            currenttargetR = downcastHitInfo.transform;
        else
            currenttargetL = downcastHitInfo.transform;

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

    private void Grab(bool right)
    {
        if (!initialized) return;

        if (!right)
        {
            grabwaitL = false;
            leftAnchor.linearLimit = on;
            if (currenttargetL != null)
            {
                GrabTrigger trigL = currenttargetL.GetComponent<GrabTrigger>();
                if (trigL != null)
                    trigL.GrabEvent();
                Rigidbody rb = currenttargetL.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    leftAnchorFixed = leftAnchor.gameObject.AddComponent<FixedJoint>();
                    leftAnchorFixed.connectedBody = rb;
                    leftAnchorRB.isKinematic = false;
                    grabbedRB_L = rb;
                }
            }
            grabL = true;
        }
        else
        {
            grabwaitR = false;
            rightAnchor.linearLimit = on;
            if (currenttargetR != null)
            {
                GrabTrigger trigR = currenttargetR.GetComponent<GrabTrigger>();
                if (trigR != null)
                    trigR.GrabEvent();
                Rigidbody rb = currenttargetR.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rightAnchorFixed = rightAnchor.gameObject.AddComponent<FixedJoint>();
                    rightAnchorFixed.connectedBody = rb;
                    rightAnchorRB.isKinematic = false;
                    grabbedRB_R = rb;
                }
            }
            grabR = true;
        }
    }

    private void Ungrab(bool right)
    {
        if (!initialized) return;

        if (!right)
        {
            leftAnchor.linearLimit = off;
            if (currenttargetL != null)
            {
                GrabTrigger trigL = currenttargetL.GetComponent<GrabTrigger>();
                if (trigL != null)
                    trigL.UngrabEvent();
                if (leftAnchorFixed != null)
                    Destroy(leftAnchorFixed);
                leftAnchorRB.isKinematic = true;
                grabbedRB_L = null;
            }
            grabL = false;
        }
        else
        {
            rightAnchor.linearLimit = off;
            if (currenttargetR != null)
            {
                GrabTrigger trigR = currenttargetR.GetComponent<GrabTrigger>();
                if (trigR != null)
                    trigR.UngrabEvent();
                if (rightAnchorFixed != null) 
                    Destroy(rightAnchorFixed);
                rightAnchorRB.isKinematic = true;
                grabbedRB_R = null;
            }
            grabR = false;
        }

        Vector3 dir = Vector3.ClampMagnitude(PlayerInfo.mainBody.velocity, 1);
        PlayerInfo.mainBody.AddForce(dir * ungrabBoost, ForceMode.Acceleration);
        PlayerInfo.physBodyRB.angularDrag = hipdrag;
        PlayerInfo.physHeadRB.angularDrag = headdrag;
    }
}
