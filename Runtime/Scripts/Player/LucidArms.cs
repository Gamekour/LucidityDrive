using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LucidArms : MonoBehaviour
{
    [SerializeField] ConfigurableJoint leftanchor;
    [SerializeField] ConfigurableJoint rightanchor;
    [SerializeField] float shoulderdist = 0.5f;
    [SerializeField] float castdist = 1;
    [SerializeField] float limitdist = 1;
    [SerializeField] float shoulderstiffness = 0.1f;
    [SerializeField] float pullstrength = 2f;
    [SerializeField] float firstcastwidth = 0.25f;
    [SerializeField] float secondcastwidth = 0.025f;
    [SerializeField] float runmaxY = 0.3f;
    [SerializeField] float minflatgrab = 0.8f;
    [SerializeField] float tolerance = 0.15f;
    [SerializeField] float swingforce = 1f;
    [SerializeField] float ungrabBoost = 1f;
    [SerializeField] Transform leftunrotate;
    [SerializeField] Transform rightunrotate;
    private Transform leftshoulder;
    private Transform rightshoulder;
    private Transform currenttargetL;
    private Transform currenttargetR;
    private SoftJointLimit off = new SoftJointLimit();
    private SoftJointLimit on = new SoftJointLimit();
    private bool grabL = false;
    private bool grabR = false;
    private bool grabwaitL = false;
    private bool grabwaitR = false;
    private bool disabling = false;
    private bool initialized = false;
    private float hipdrag = 0;
    private float headdrag = 0;

    private void OnEnable()
    {
        if (LucidInputActionRefs.grabL != null)
        {
            LucidInputActionRefs.grabL.started += GrabButtonLeft;
            LucidInputActionRefs.grabL.canceled += UngrabButtonLeft;
            LucidInputActionRefs.grabR.started += GrabButtonRight;
            LucidInputActionRefs.grabR.canceled += UngrabButtonRight;
        }
        if (PlayerInfo.vismodelRef != null)
            Init();
        else
            StartCoroutine(InitDelay());
    }

    private void Init()
    {
        disabling = false;
        leftshoulder = PlayerInfo.vismodelRef.anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightshoulder = PlayerInfo.vismodelRef.anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        leftanchor.connectedBody = PlayerInfo.mainBody;
        rightanchor.connectedBody = PlayerInfo.mainBody;
        hipdrag = PlayerInfo.physHipsRB.angularDrag;
        headdrag = PlayerInfo.physHeadRB.angularDrag;
        initialized = true;
    }

    private void OnDisable()
    {
        disabling = true;
        grabwaitL = false; 
        grabwaitR = false;
        Ungrab(false);
        Ungrab(true);
        PlayerInfo.climbing = false;
        LucidInputActionRefs.grabL.started -= GrabButtonLeft;
        LucidInputActionRefs.grabL.canceled -= UngrabButtonLeft;
        LucidInputActionRefs.grabR.started -= GrabButtonRight;
        LucidInputActionRefs.grabR.canceled -= UngrabButtonRight;
    }

    private void Start()
    {
        LucidInputActionRefs.grabL.started += GrabButtonLeft;
        LucidInputActionRefs.grabL.canceled += UngrabButtonLeft;
        LucidInputActionRefs.grabR.started += GrabButtonRight;
        LucidInputActionRefs.grabR.canceled += UngrabButtonRight;
        off.limit = Mathf.Infinity;
        on.limit = limitdist;
        leftanchor.linearLimit = off;
        rightanchor.linearLimit = off;
    }

    //in short: uses an initial cast to find the nearest "wall", then another cast to find the top of said wall. If all is good and within reach, then we start asking if we're trying to grab and if so we call those functions up
    private void FixedUpdate()
    {
        if (disabling || leftshoulder == null || rightshoulder == null || !initialized) return;

        GrabLogic(false);
        GrabLogic(true);

        CalcClimbRelative();

        Vector3 targetposL = PlayerInfo.pelvis.transform.InverseTransformPoint(leftshoulder.position);
        Vector3 targetposR = PlayerInfo.pelvis.transform.InverseTransformPoint(rightshoulder.position);

        leftanchor.connectedAnchor += (targetposL - leftanchor.connectedAnchor) * shoulderstiffness;
        rightanchor.connectedAnchor += (targetposR - rightanchor.connectedAnchor) * shoulderstiffness;

        PlayerInfo.grabL = grabL;
        PlayerInfo.grabR = grabR;
        PlayerInfo.climbing = grabL || grabR;

        if (PlayerInfo.climbing)
        {
            Vector2 inputmove = LucidInputValueShortcuts.movement;
            Vector3 moveflat = Vector3.zero;
            moveflat.x = inputmove.x;
            moveflat.z = inputmove.y;
            PlayerInfo.mainBody.AddForce(PlayerInfo.pelvis.TransformVector(moveflat) * swingforce);
        }

        if (PlayerInfo.grabR)
        {
            PlayerInfo.IK_RH.position = rightanchor.transform.position;
            PlayerInfo.IK_RH.rotation = rightanchor.transform.rotation;
        }
        if (PlayerInfo.grabL)
        {
            PlayerInfo.IK_LH.position = leftanchor.transform.position;
            PlayerInfo.IK_LH.rotation = leftanchor.transform.rotation;
        }
    }

    private void GrabLogic(bool right)
    {
        bool pulling = LucidInputValueShortcuts.crouch;
        bool grabToCheck = (right ? grabR : grabL);
        ConfigurableJoint targetanchor = right ? rightanchor : leftanchor;

        if (!grabToCheck)
        {
            targetanchor.transform.position = PlayerInfo.pelvis.position;

            bool validgrab = ClimbScan(right);

            Vector3 targetposL = PlayerInfo.pelvis.transform.InverseTransformPoint(leftshoulder.position);
            Vector3 targetposR = PlayerInfo.pelvis.transform.InverseTransformPoint(rightshoulder.position);

            bool oldgrab = right ? PlayerInfo.validgrabR : PlayerInfo.validgrabL;
            if (right)
                PlayerInfo.validgrabR = validgrab;
            else
                PlayerInfo.validgrabL = validgrab;

            bool grabwait = (right ? grabwaitR : grabwaitL);
            if (!oldgrab && validgrab && grabwait)
            {
                Grab(right);
            }

            Transform shoulder = right ? rightshoulder : leftshoulder;

            bool toofar = false;
            if (toofar) //ungrab if something pushes you far enough to "lose grip"
                Ungrab(right);
        }
        else
        {
            if (right)
                PlayerInfo.validgrabR = true;
            else
                PlayerInfo.validgrabL = true;
        }

        if (PlayerInfo.climbing)
            targetanchor.targetPosition = Vector3.up * (pulling && grabToCheck ? 1 : 0) * pullstrength;
    }

    private bool ClimbScan(bool right)
    {
        ConfigurableJoint jointTarget = right ? rightanchor : leftanchor;
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

        Vector3 shoulder = campos + (right ? camright * shoulderdist : -camright * shoulderdist);

        bool initialHit = Physics.SphereCast(shoulder, firstcastwidth, camfwd, out RaycastHit initialHitInfo, castdist, Shortcuts.geometryMask);
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
        float sinC = (dist * Mathf.Sin(Mathf.Deg2Rad * angle)) / castdist;
        float A = 180 - (Mathf.Asin(sinC) + angle);
        float newmaxheight = (Mathf.Sin(Mathf.Deg2Rad * A) * castdist) / Mathf.Sin(Mathf.Deg2Rad * angle);

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

    private void Grab(bool right)
    {
        if (!initialized) return;

        if (!right)
        {
            grabwaitL = false;
            leftanchor.linearLimit = on;
            if (currenttargetL != null)
            {
                GrabTrigger trigL = currenttargetL.GetComponent<GrabTrigger>();
                if (trigL != null)
                    trigL.GrabEvent();
            }
            grabL = true;
        }
        else
        {
            grabwaitR = false;
            rightanchor.linearLimit = on;
            if (currenttargetR != null)
            {
                GrabTrigger trigR = currenttargetR.GetComponent<GrabTrigger>();
                if (trigR != null)
                    trigR.GrabEvent();
            }
            grabR = true;
        }

        PlayerInfo.physHipsRB.angularDrag = 0;
        PlayerInfo.physHeadRB.angularDrag = 0;
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

    private void Ungrab(bool right)
    {
        if (!initialized) return;

        Animator animL = null;
        Animator animR = null;
        if (!right)
        {
            leftanchor.linearLimit = off;
            if (currenttargetL != null)
            {
                animL = currenttargetL.GetComponent<Animator>();
                if (animL != null)
                    animL.SetBool("grabbed", false);
            }
            grabL = false;
        }
        else
        {
            rightanchor.linearLimit = off;
            if (currenttargetR != null)
            {
                animR = currenttargetR.GetComponent<Animator>();
                if (animR != null && animR != animL)
                    animR.SetBool("grabbed", false);
            }
            grabR = false;
        }

        Vector3 dir = Vector3.ClampMagnitude(PlayerInfo.mainBody.velocity, 1);
        PlayerInfo.mainBody.AddForce(dir * ungrabBoost, ForceMode.Acceleration);
        PlayerInfo.physHipsRB.angularDrag = hipdrag;
        PlayerInfo.physHeadRB.angularDrag = headdrag;
    }

    private IEnumerator InitDelay()
    {
        yield return new WaitForSeconds(1);
        Init();
    }
}
