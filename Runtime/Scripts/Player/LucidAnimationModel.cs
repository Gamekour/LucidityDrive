using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.XR;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Animator))]
public class LucidAnimationModel : MonoBehaviour
{
    [SerializeField] Camera fpcam;
    [SerializeField] float velsmoothness = 0.5f;
    [SerializeField] float velscale = 0.75f;
    [SerializeField] float nrmsmoothness = 0.5f;
    [SerializeField] float footsmoothness = 0.5f;
    [SerializeField] float willsmoothness = 0.5f;
    [SerializeField] float alignmentsmoothness = 0.5f;
    [SerializeField] float midaircrouchsmoothness = 0.5f;
    [SerializeField] float midaircrouch = 0.5f;
    [SerializeField] float castthickness = 0.07f;
    [SerializeField] float castheight = 0.07f;
    [SerializeField] float airtimemax = 1;
    [SerializeField] float groundedforgiveness = 1;
    [SerializeField] float lerp = 0.5f;
    [SerializeField] float swaymult;
    [SerializeField] float swayspeed;
    [SerializeField] float landspeed = 0.5f;
    [SerializeField] float footslidethreshold = 3f;
    [SerializeField] float footslidevelthreshold = 3f;
    [SerializeField] float leansmoothness = 0.5f;
    [HideInInspector]
    public Dictionary<string, Quaternion> boneRots = new Dictionary<string, Quaternion>();
    [HideInInspector]
    public Vector3 LCast;
    [HideInInspector]
    public Vector3 RCast;
    public Quaternion hiprot = Quaternion.identity;
    private Animator anim;
    private Transform pelvis;
    private Transform head;
    private float currentcrouch = 0;
    private float currentsway = 0;
    private float airtimesmooth = 0;
    private bool stucksliding = false;
    private Transform animpelvis;
    private Transform animFootL;
    private Transform animFootR;
    private Transform animKneeL;
    private Transform animKneeR;
    private Transform animHandL;
    private Transform animHandR;
    private Transform animShoulderL;
    private Transform animShoulderR;

    private void Awake()
    {
        PlayerInfo.playermodelAnim = GetComponent<Animator>();
    }
    private void Start()
    {
        anim = PlayerInfo.playermodelAnim;
        pelvis = PlayerInfo.pelvis;
        head = PlayerInfo.head;

        animpelvis = anim.GetBoneTransform(HumanBodyBones.Hips);
        animFootL = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        animFootR = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        animKneeL = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        animKneeR = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        animHandL = anim.GetBoneTransform(HumanBodyBones.LeftHand);
        animHandR = anim.GetBoneTransform(HumanBodyBones.RightHand);
        animShoulderL = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        animShoulderR = anim.GetBoneTransform (HumanBodyBones.RightUpperArm);
        PlayerInfo.mainCamera = fpcam;
    }
    private void Update()
    {
        transform.position += pelvis.position - animpelvis.position;
        transform.rotation = pelvis.rotation;
        currentsway = Mathf.Sin(Time.time * swayspeed) * swaymult;
    }

    private void OnAnimatorMove()
    {
        CollectBoneRotations();
        hiprot = anim.GetBoneTransform(HumanBodyBones.Hips).rotation;
    }

    //reports all major bone rotations, mostly for vismodel sync - maybe there's a more optimized way to do this via constraints?
    private void CollectBoneRotations()
    {
        foreach (HumanBodyBones hb2 in Shortcuts.hb2list)
        {
            Transform t = anim.GetBoneTransform(hb2);
            string hbstring = Enum.GetName(typeof(HumanBodyBones), hb2);
            if (!boneRots.ContainsKey(hbstring))
            {
                if (hb2 != HumanBodyBones.Hips)
                    boneRots.Add(hbstring, t.localRotation);
            }
            else
            {
                if (hb2 != HumanBodyBones.Hips)
                    boneRots[hbstring] = t.localRotation;
            }
        }
    }

    //there really isnt any way i know of to refactor this without passing a thousand variables around
    private void OnAnimatorIK(int layerIndex)
    {
        bool currentright = PlayerInfo.animphase < 0.5f;

        Vector2 moveVector = LucidInputActionRefs.movement.ReadValue<Vector2>();
        Vector3 moveFlat = Vector3.zero;
        moveFlat.x = moveVector.x;
        moveFlat.z = moveVector.y;

        Vector3 localVel = PlayerInfo.hipspace.InverseTransformVector(PlayerInfo.mainBody.velocity);
        Vector3 velflat = localVel;
        velflat.y = 0;
        localVel *= velscale;
        Vector3 currentvellocal = new Vector3(anim.GetFloat("velX"), anim.GetFloat("velY"), anim.GetFloat("velZ"));
        localVel = Vector3.Lerp(localVel, currentvellocal, velsmoothness);
        Vector3 localNrm = PlayerInfo.pelvis.InverseTransformVector(PlayerInfo.hipspace.up);
        Vector3 currentnrmlocal = new Vector3(anim.GetFloat("nrmX"), anim.GetFloat("nrmY"), anim.GetFloat("nrmZ"));
        localNrm = Vector3.Lerp(currentnrmlocal, localNrm,1 - nrmsmoothness);
        Vector3 currentWill = new Vector3(anim.GetFloat("willX"), 0, anim.GetFloat("willZ"));
        Vector3 willFlat = Vector3.Lerp(currentWill, moveFlat, willsmoothness);
        float lastalignment = anim.GetFloat("alignment");
        lastalignment = Mathf.Lerp(PlayerInfo.alignment, lastalignment, alignmentsmoothness);

        bool crawl = LucidInputValueShortcuts.crawl;
        bool slide = LucidInputValueShortcuts.slide;
        if (slide || crawl)
            stucksliding = true;

        bool footslide = (lastalignment > footslidethreshold && localVel.magnitude > footslidevelthreshold);

        Vector2 lean;
        Vector2 oldlean = Vector2.zero;
        oldlean.x = anim.GetFloat("leanX");
        oldlean.y = anim.GetFloat("leanZ");
        lean = LeanCalc(moveFlat, localVel * 0.25f, localNrm);
        Vector2 lerplean = Vector2.zero;
        lerplean = Vector2.Lerp(oldlean, lean, leansmoothness);
        anim.SetFloat("leanX", lerplean.x);
        anim.SetFloat("leanZ", lerplean.y);

        anim.SetFloat("velX", localVel.x);
        anim.SetFloat("velY", localVel.y);
        anim.SetFloat("velZ", localVel.z);
        anim.SetFloat("willX", willFlat.x);
        anim.SetFloat("willZ", willFlat.z);
        anim.SetFloat("nrmX", localNrm.x);
        anim.SetFloat("nrmY", localNrm.y);
        anim.SetFloat("nrmZ", localNrm.z);
        anim.SetFloat("animcycle", PlayerInfo.animphase);
        anim.SetFloat("airtime", PlayerInfo.airtime);
        anim.SetFloat("alignment", lastalignment);
        anim.SetFloat("climb", PlayerInfo.climbrelative.y);
        anim.SetFloat("hangX", PlayerInfo.climbrelative.x);
        anim.SetFloat("hangZ", PlayerInfo.climbrelative.z);
        anim.SetBool("grounded", PlayerInfo.grounded);
        anim.SetBool("slide",  slide);
        anim.SetBool("bslide", crawl);
        anim.SetBool("crawl", PlayerInfo.crawling);
        anim.SetBool("flight", PlayerInfo.flying);
        anim.SetBool("grabL", PlayerInfo.grabL);
        anim.SetBool("grabR", PlayerInfo.grabR);
        anim.SetBool("climbing", PlayerInfo.climbing);
        anim.SetBool("footslide", footslide);
        if (PlayerInfo.airtime > airtimesmooth)
            airtimesmooth = PlayerInfo.airtime;
        else
            airtimesmooth = Mathf.Lerp(airtimesmooth, PlayerInfo.airtime, landspeed);

        float legweight = Mathf.Clamp01(airtimesmooth / airtimemax);
        legweight = Mathf.Clamp01(legweight + (1 - localNrm.y));
        legweight *= (1 - (slide ? 1 : 0));
        legweight *= (1 - (crawl ? 1 : 0));
        legweight *= (1 - (PlayerInfo.flying ? 1 : 0));

        anim.SetLayerWeight(1, legweight);
        anim.SetLayerWeight(2, Mathf.Clamp01(1 - ((slide ? 1 : 0) + (crawl ? 1 : 0))) * Mathf.Clamp01(velflat.magnitude));
        anim.SetLayerWeight(3, Mathf.Clamp01(velflat.magnitude + (PlayerInfo.grounded ? 0 : 1)));

        Transform tSpine = anim.GetBoneTransform(HumanBodyBones.Spine);
        tSpine.Rotate(Vector3.forward, currentsway, Space.Self);
        Quaternion chest = anim.GetBoneTransform(HumanBodyBones.Chest).rotation;
        Quaternion localSpaceRotationNeck = Quaternion.Inverse(chest) * Quaternion.Slerp(head.rotation, chest, lerp);
        anim.SetBoneLocalRotation(HumanBodyBones.Neck, localSpaceRotationNeck);
        anim.SetBoneLocalRotation(HumanBodyBones.Head, localSpaceRotationNeck);

        Vector3 footposL = animFootL.position;
        Vector3 footposR = animFootR.position;
        Vector3 kneeposL = animKneeL.position;
        Vector3 kneeposR = animKneeR.position;

        bool midaircrouching = (!PlayerInfo.grounded && LucidInputActionRefs.crouch.ReadValue<float>() == 1 && !slide && !crawl && !PlayerInfo.flying);

        float targetcrouch = midaircrouch;
        if (!midaircrouching)
            targetcrouch = 0;

        currentcrouch = Mathf.Lerp(targetcrouch, currentcrouch, midaircrouchsmoothness);

        RaycastHit LHitInfoThigh = new RaycastHit();
        RaycastHit RHitInfoThigh = new RaycastHit();
        RaycastHit LHitInfoShin = new RaycastHit();
        RaycastHit RHitInfoShin = new RaycastHit();

        bool thighCastL = Physics.SphereCast(PlayerInfo.legspaceL.position + (PlayerInfo.legspaceL.up * castheight), castthickness, kneeposL - PlayerInfo.legspaceL.position, out LHitInfoThigh, Vector3.Distance(footposL, PlayerInfo.legspaceL.position), Shortcuts.geometryMask);
        bool thighCastR = Physics.SphereCast(PlayerInfo.legspaceR.position + (PlayerInfo.legspaceR.up * castheight), castthickness, kneeposR - PlayerInfo.legspaceR.position, out RHitInfoThigh, Vector3.Distance(footposR, PlayerInfo.legspaceR.position), Shortcuts.geometryMask);
        bool shinCastL = Physics.SphereCast(kneeposL, castthickness, footposL - PlayerInfo.legspaceL.position, out LHitInfoShin, Vector3.Distance(footposL, PlayerInfo.legspaceL.position) * groundedforgiveness, Shortcuts.geometryMask);
        bool shinCastR = Physics.SphereCast(kneeposR, castthickness, footposR - PlayerInfo.legspaceR.position, out RHitInfoShin, Vector3.Distance(footposR, PlayerInfo.legspaceR.position) * groundedforgiveness, Shortcuts.geometryMask);
        Vector3 LCastOld = LCast;
        if (thighCastL)
        {
            LCast = LHitInfoThigh.point;
            if (!currentright || !thighCastR)
            {
                PlayerInfo.footsurface = LHitInfoThigh.normal;
                PlayerInfo.footsurfL = LHitInfoThigh.normal;
                PlayerInfo.footspace.position = LHitInfoThigh.point;
                PlayerInfo.footspace.up = LHitInfoThigh.normal;
            }
        }
        else if (shinCastL)
        {
            LCast = LHitInfoShin.point;
            if (!currentright || !shinCastR)
            {
                PlayerInfo.footsurface = LHitInfoShin.normal;
                PlayerInfo.footsurfL = LHitInfoShin.normal;
                PlayerInfo.footspace.position = LHitInfoShin.point;
                PlayerInfo.footspace.up = LHitInfoShin.normal;
            }
        }
        else
            LCast = footposL;
        LCast = Vector3.Lerp(LCast, LCastOld, footsmoothness);

        Vector3 RCastOld = RCast;
        if (thighCastR)
        {
            RCast = RHitInfoThigh.point;
            if (currentright || !thighCastL)
            {
                PlayerInfo.footsurface = RHitInfoThigh.normal;
                PlayerInfo.footsurfR = RHitInfoThigh.normal;
                PlayerInfo.footspace.position = RHitInfoThigh.point;
                PlayerInfo.footspace.up = RHitInfoThigh.normal;
            }
        }
        else if (shinCastR)
        {
            RCast = RHitInfoShin.point;
            if (currentright || !shinCastL)
            {
                PlayerInfo.footsurface = RHitInfoShin.normal;
                PlayerInfo.footsurfR = RHitInfoShin.normal;
                PlayerInfo.footspace.position = RHitInfoShin.point;
                PlayerInfo.footspace.up = RHitInfoShin.normal;
            }
        }
        else
            RCast = footposR;
        RCast = Vector3.Lerp(RCast, RCastOld, footsmoothness);

        bool castsuccess = (thighCastL || thighCastR || shinCastL || shinCastR);

        PlayerInfo.grounded = castsuccess || PlayerInfo.pelviscollision;

        if (PlayerInfo.crawling)
            PlayerInfo.footsurface = PlayerInfo.hipspace.up;

        PlayerInfo.footTargetL = LCast;
        PlayerInfo.footTargetR = RCast;

        bool armLHit = Physics.SphereCast(animShoulderL.position, castthickness / 2, animHandL.position - animShoulderL.position, out RaycastHit armHitInfoL, Vector3.Distance(animHandL.position, animShoulderL.position), Shortcuts.geometryMask);
        bool armRHit = Physics.SphereCast(animShoulderR.position, castthickness / 2, animHandR.position - animShoulderR.position, out RaycastHit armHitInfoR, Vector3.Distance(animHandR.position, animShoulderR.position), Shortcuts.geometryMask);

        if (armLHit)
            PlayerInfo.handTargetL = armHitInfoL.point;
        else
            PlayerInfo.handTargetL = Vector3.zero;
        if (armRHit)
            PlayerInfo.handTargetR = armHitInfoR.point;
        else
            PlayerInfo.handTargetR = Vector3.zero;
    }

    private Vector2 LeanCalc(Vector3 localwill, Vector3 localvel, Vector3 localnrm, float k1 = 0.3f, float k2 = 0.7f)
    {
        Vector2 accel = Vector2.zero;
        accel.x = localvel.x;
        accel.y = localvel.z;
        Vector2 slope = Vector2.zero;
        slope.x = SlopeLeanCalc(localnrm.x);
        slope.y = SlopeLeanCalc(localnrm.z);
        Vector2 lean = Vector2.zero;
        lean.x = (accel.x * k1) + (slope.x * k2);
        lean.y = (accel.y * k1) + (slope.y * k2);

        if (PlayerInfo.alignment > 0.5f && PlayerInfo.movespeed > footslidethreshold && PlayerInfo.grounded)
            lean = -lean;

        return lean;
    }

    public float SlopeLeanCalc(float slope)
    {
        if (Mathf.Abs(slope) < 0.75f) return -1.333f * slope;
        else
            return slope;
    }

    public void OnReachedGroundedState()
    {
        stucksliding = false;
    }
}
