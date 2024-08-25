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
        PlayerInfo.climbtargetL = leftanchor.transform;
        PlayerInfo.climbtargetR = rightanchor.transform;
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

        if (!grabL)
            leftanchor.transform.position = PlayerInfo.pelvis.position;
        if(!grabR)
            rightanchor.transform.position = PlayerInfo.pelvis.position;


        bool pulling = LucidInputValueShortcuts.crouch;

        Transform cam = PlayerInfo.head;

        bool initialHitL = Physics.SphereCast(cam.position - (cam.right * shoulderdist), firstcastwidth, cam.forward, out RaycastHit initialHitInfoL, castdist, Shortcuts.geometryMask);
        initialHitL &= ((initialHitInfoL.normal.y) < runmaxY);

        bool initialHitR = Physics.SphereCast(cam.position + (cam.right * shoulderdist), firstcastwidth, cam.forward, out RaycastHit initialHitInfoR, castdist, Shortcuts.geometryMask);
        initialHitR &= ((initialHitInfoR.normal.y) < runmaxY);

        float hitZL = PlayerInfo.pelvis.InverseTransformPoint(initialHitInfoL.point).z / 2;
        float hitZR = PlayerInfo.pelvis.InverseTransformPoint(initialHitInfoR.point).z / 2;

        Vector3 projectvectorL = Vector3.up;
        if (initialHitInfoL.normal.y < -0.05f)
            projectvectorL = -PlayerInfo.pelvis.forward + Vector3.up;
        else if (initialHitInfoL.normal.y > 0.05f)
            projectvectorL = PlayerInfo.pelvis.forward + Vector3.up;

        Vector3 projectvectorR = Vector3.up;
        if (initialHitInfoR.normal.y < -0.05f)
            projectvectorR = -PlayerInfo.pelvis.forward + Vector3.up;
        else if (initialHitInfoR.normal.y > 0.05f)
            projectvectorR = PlayerInfo.pelvis.forward + Vector3.up;

        Vector3 hitvectorL = Vector3.ProjectOnPlane(projectvectorL, initialHitInfoL.normal).normalized;
        if (Mathf.Abs(initialHitInfoL.normal.y) < 0.01f)
            hitvectorL = Vector3.up;
        Vector3 hitvectorR = Vector3.ProjectOnPlane(projectvectorR, initialHitInfoR.normal).normalized;
        if (Mathf.Abs(initialHitInfoR.normal.y) < 0.01f)
            hitvectorR = Vector3.up;

        float angleL = Vector3.Angle(-cam.forward, hitvectorL);
        float angleR = Vector3.Angle(-cam.forward, hitvectorR);

        float distL = Vector3.Distance(cam.position + cam.right * -shoulderdist, initialHitInfoL.point);
        float distR = Vector3.Distance(cam.position + cam.right * shoulderdist, initialHitInfoR.point);


        //i know this trig stuff looks scary but that's just how it knows where to look when you're dealing with a sloped wall
        float LsinC = (distL * Mathf.Sin(Mathf.Deg2Rad * angleL)) / castdist;
        float RsinC = (distR * Mathf.Sin(Mathf.Deg2Rad * angleR)) / castdist;

        float LA = 180 - (Mathf.Asin(LsinC) + angleL);
        float RA = 180 - (Mathf.Asin(RsinC) + angleR);

        float newmaxheightL = (Mathf.Sin(Mathf.Deg2Rad * LA) * castdist) / Mathf.Sin(Mathf.Deg2Rad * angleL);
        float newmaxheightR = (Mathf.Sin(Mathf.Deg2Rad * RA) * castdist) / Mathf.Sin(Mathf.Deg2Rad * angleR);

        bool upCastHitL = Physics.Raycast(initialHitInfoL.point, hitvectorL, out RaycastHit upCastHitInfoL, newmaxheightL, Shortcuts.geometryMask);
        bool upCastHitR = Physics.Raycast(initialHitInfoR.point, hitvectorR, out RaycastHit upCastHitInfoR, newmaxheightR, Shortcuts.geometryMask);

        Vector3 startpointL = initialHitInfoL.point + hitvectorL * newmaxheightL;
        if (upCastHitL)
            startpointL = upCastHitInfoL.point;
        Vector3 startpointR = initialHitInfoR.point + hitvectorR * newmaxheightR;
        if (upCastHitR)
            startpointR = upCastHitInfoR.point;

        bool surfaceCastHitL = Physics.SphereCast(startpointL - initialHitInfoL.normal * secondcastwidth, secondcastwidth, -hitvectorL, out RaycastHit downcastHitInfoL, newmaxheightL, Shortcuts.geometryMask);
        bool surfaceCastHitR = Physics.SphereCast(startpointR - initialHitInfoR.normal * secondcastwidth, secondcastwidth, -hitvectorR, out RaycastHit downcastHitInfoR, newmaxheightR, Shortcuts.geometryMask);

        bool validgrabL = initialHitL && surfaceCastHitL && downcastHitInfoL.normal.y > minflatgrab;
        bool validgrabR = initialHitR && surfaceCastHitR && downcastHitInfoR.normal.y > minflatgrab;

        if (!grabL)
        {
            if (validgrabL)
            {
                leftanchor.transform.position = downcastHitInfoL.point;
                Vector3 transformhitinfo = Vector3.ProjectOnPlane(-initialHitInfoL.normal, downcastHitInfoL.normal);
                leftanchor.transform.rotation = Quaternion.LookRotation(transformhitinfo, downcastHitInfoL.normal);
            }
            leftanchor.targetPosition = Vector3.zero;

            bool toofarL = Vector3.Distance(leftanchor.transform.position, leftshoulder.position) > limitdist + tolerance;

            if (toofarL) //ungrab if something pushes you far enough to "lose grip"
                Ungrab(false);

            currenttargetL = downcastHitInfoL.transform;
        }
        else
            leftanchor.targetPosition = Vector3.up * (pulling ? 1 : 0) * pullstrength;

        if (!grabR)
        {
            if (validgrabR)
            {
                rightanchor.transform.position = downcastHitInfoR.point;
                Vector3 transformhitinfo = Vector3.ProjectOnPlane(-initialHitInfoR.normal, downcastHitInfoR.normal);
                rightanchor.transform.rotation = Quaternion.LookRotation(transformhitinfo, downcastHitInfoR.normal);
            }
            rightanchor.targetPosition = Vector3.zero;

            bool toofarR = Vector3.Distance(rightanchor.transform.position, rightshoulder.position) > limitdist + tolerance;

            if (toofarR) //ungrab if something pushes you far enough to "lose grip"
                Ungrab(true);

            currenttargetR = downcastHitInfoR.transform;
        }
        else
        {
            rightanchor.targetPosition = Vector3.up * (pulling ? 1 : 0) * pullstrength;
        }

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

        Vector3 targetposL = PlayerInfo.pelvis.transform.InverseTransformPoint(leftshoulder.position);
        Vector3 targetposR = PlayerInfo.pelvis.transform.InverseTransformPoint(rightshoulder.position);

        leftanchor.connectedAnchor += (targetposL - leftanchor.connectedAnchor) * shoulderstiffness;
        rightanchor.connectedAnchor += (targetposR - rightanchor.connectedAnchor) * shoulderstiffness;

        bool oldgrabL = PlayerInfo.validgrabL;
        PlayerInfo.validgrabL = validgrabL;

        if (!oldgrabL && validgrabL && grabwaitL)
        {
            Grab(false);
        }

        bool oldgrabR = PlayerInfo.validgrabR;
        PlayerInfo.validgrabR = validgrabR;

        if (!oldgrabR && validgrabR && grabwaitR)
        {
            Grab(true);
        }

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

        PlayerInfo.physHipsRB.angularDrag = hipdrag;
        PlayerInfo.physHeadRB.angularDrag = headdrag;
    }

    private IEnumerator InitDelay()
    {
        yield return new WaitForSeconds(1);
        Init();
    }
}
