using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [HideInInspector]
    public Dictionary<string, Quaternion> boneRots = new Dictionary<string, Quaternion>();
    [HideInInspector]
    public Vector3 LCast;
    [HideInInspector]
    public Vector3 RCast;
    public Quaternion hiprot = Quaternion.identity;
    private Animator anim;
    private Transform pelvis;
    private Transform animpelvis;
    private Transform head;
    private float currentcrouch = 0;
    private float currentsway = 0;
    private float airtimesmooth = 0;
    private bool stucksliding = false;

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
        localVel *= velscale;
        Vector3 currentvellocal = new Vector3(anim.GetFloat("velX"), anim.GetFloat("velY"), anim.GetFloat("velZ"));
        localVel = Vector3.Lerp(localVel, currentvellocal, velsmoothness);
        Vector3 localNrm = PlayerInfo.pelvis.InverseTransformVector(PlayerInfo.hipspace.up);
        Vector3 currentnrmlocal = new Vector3(anim.GetFloat("nrmX"), anim.GetFloat("nrmY"), anim.GetFloat("nrmZ"));
        localNrm = Vector3.Lerp(localNrm, currentnrmlocal, nrmsmoothness);
        Vector3 currentWill = new Vector3(anim.GetFloat("willX"), 0, anim.GetFloat("willZ"));
        Vector3 willFlat = Vector3.Lerp(moveFlat, currentWill, willsmoothness);
        float lastalignment = anim.GetFloat("alignment");
        lastalignment = Mathf.Lerp(PlayerInfo.alignment, lastalignment, alignmentsmoothness);

        bool crawl = LucidInputValueShortcuts.crawl;
        bool slide = LucidInputValueShortcuts.slide;
        float flightfloat = 0;
        if (PlayerInfo.flying)
            flightfloat = 1;
        if (slide || crawl)
            stucksliding = true;

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
        if (PlayerInfo.airtime > airtimesmooth)
            airtimesmooth = PlayerInfo.airtime;
        else
            airtimesmooth = Mathf.Lerp(airtimesmooth, PlayerInfo.airtime, landspeed);
        anim.SetLayerWeight(1, Mathf.Clamp01(airtimesmooth / airtimemax) * (1 - (slide ? 1 : 0)) * (1 - (crawl ? 1 : 0)) * (1 - flightfloat));

        Transform tSpine = anim.GetBoneTransform(HumanBodyBones.Spine);
        tSpine.Rotate(Vector3.forward, currentsway, Space.Self);
        Quaternion chest = anim.GetBoneTransform(HumanBodyBones.Chest).rotation;
        Quaternion localSpaceRotationNeck = Quaternion.Inverse(chest) * Quaternion.Slerp(head.rotation, chest, lerp);
        anim.SetBoneLocalRotation(HumanBodyBones.Neck, localSpaceRotationNeck);
        anim.SetBoneLocalRotation(HumanBodyBones.Head, localSpaceRotationNeck);
        Vector3 targetL = anim.GetBoneTransform(HumanBodyBones.LeftFoot).position;
        Vector3 targetR = anim.GetBoneTransform(HumanBodyBones.RightFoot).position;

        bool midaircrouching = (!PlayerInfo.grounded && LucidInputActionRefs.crouch.ReadValue<float>() == 1 && !slide && !crawl && !PlayerInfo.flying);

        float targetcrouch = midaircrouch;
        if (!midaircrouching)
            targetcrouch = 0;

        currentcrouch = Mathf.Lerp(targetcrouch, currentcrouch, midaircrouchsmoothness);

        if (!PlayerInfo.grounded)
        {
            targetL = Vector3.Lerp(targetL, PlayerInfo.legspaceL.position, currentcrouch * Mathf.Clamp01(PlayerInfo.airtime / airtimemax));
            targetR = Vector3.Lerp(targetR, PlayerInfo.legspaceR.position, currentcrouch * Mathf.Clamp01(PlayerInfo.airtime / airtimemax));
        }

        RaycastHit LHitInfo = new RaycastHit();
        RaycastHit RHitInfo = new RaycastHit();

        bool LHit = Physics.SphereCast(PlayerInfo.legspaceL.position + (PlayerInfo.legspaceL.up * castheight), castthickness, targetL - PlayerInfo.legspaceL.position, out LHitInfo, Vector3.Distance(targetL, PlayerInfo.legspaceL.position) * groundedforgiveness, Shortcuts.geometryMask);
        bool Rhit = Physics.SphereCast(PlayerInfo.legspaceR.position + (PlayerInfo.legspaceR.up * castheight), castthickness, targetR - PlayerInfo.legspaceR.position, out RHitInfo, Vector3.Distance(targetR, PlayerInfo.legspaceR.position) * groundedforgiveness, Shortcuts.geometryMask);
        if (PlayerInfo.crawling || crawl)
        {
            Vector3 newtargetR = anim.GetBoneTransform(HumanBodyBones.RightFoot).position;
            Vector3 newtargetL = anim.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            LHit = Physics.SphereCast(PlayerInfo.legspaceL.position + (PlayerInfo.legspaceL.up * castheight), castthickness, newtargetL - PlayerInfo.legspaceL.position, out LHitInfo, Vector3.Distance(newtargetL, PlayerInfo.legspaceL.position) * groundedforgiveness, Shortcuts.geometryMask);
            Rhit = Physics.SphereCast(PlayerInfo.legspaceR.position + (PlayerInfo.legspaceR.up * castheight), castthickness, newtargetR - PlayerInfo.legspaceR.position, out RHitInfo, Vector3.Distance(newtargetR, PlayerInfo.legspaceR.position) * groundedforgiveness, Shortcuts.geometryMask);
            Debug.DrawLine(PlayerInfo.legspaceL.position, newtargetL);

        }
        Vector3 LCastOld = LCast;
        if (LHit)
        {
            LCast = LHitInfo.point;
            if (!currentright || !Rhit)
            {
                PlayerInfo.footsurface = LHitInfo.normal;
                PlayerInfo.footsurfL = LHitInfo.normal;
                PlayerInfo.footspace.position = LHitInfo.point;
                PlayerInfo.footspace.up = LHitInfo.normal;
            }
        }
        else
            LCast = targetL;
        LCast = Vector3.Lerp(LCast, LCastOld, footsmoothness);

        Vector3 RCastOld = RCast;
        if (Rhit)
        {
            RCast = RHitInfo.point;
            if (currentright || !LHit)
            {
                PlayerInfo.footsurface = RHitInfo.normal;
                PlayerInfo.footsurfR = RHitInfo.normal;
                PlayerInfo.footspace.position = RHitInfo.point;
                PlayerInfo.footspace.up = RHitInfo.normal;
            }
        }
        else
            RCast = targetR;
        RCast = Vector3.Lerp(RCast, RCastOld, footsmoothness);

        bool castsuccess = (LHit || Rhit);
        bool slidecheck = !(slide || crawl || stucksliding);
        PlayerInfo.grounded = (castsuccess && slidecheck) || PlayerInfo.pelviscollision;

        if (PlayerInfo.crawling)
        {
            PlayerInfo.footsurface = PlayerInfo.hipspace.up;
            //PlayerInfo.footspace.rotation = PlayerInfo.hipspace.rotation;
        }

        PlayerInfo.targetL = LCast;
        PlayerInfo.targetR = RCast;
    }

    public void OnReachedGroundedState()
    {
        stucksliding = false;
    }
}
