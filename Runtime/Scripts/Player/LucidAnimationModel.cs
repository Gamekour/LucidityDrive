using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LucidAnimationModel : MonoBehaviour
{
    public float airtimeThreshold = 0.5f;

    [SerializeField] RuntimeAnimatorController controller;
    [SerializeField] Camera fpcam;

    [SerializeField] Transform 
        IK_LF,
        IK_RF,
        IK_LH,
        IK_RH;

    [SerializeField] float 
        velsmoothTime,
        velscale,
        nrmSmoothTime,
        footSmoothTime,
        willSmoothTime,
        alignmentSmoothTime,
        castthickness,
        castheight,
        airtimemax,
        groundedforgiveness,
        lerp,
        landtime,
        footslidethreshold,
        footslidevelthreshold,
        leansmoothtime,
        leansmoothtimeLayer,
        unrotateFeetBySpeed,
        maxFootAngle,
        verticalFootAdjust,
        hipWeightReductionByHeight;

    [HideInInspector]
    public Dictionary<string, Quaternion> boneRots = new Dictionary<string, Quaternion>();
    [HideInInspector]
    public Vector3 LCast;
    [HideInInspector]
    public Vector3 RCast;
    [HideInInspector]
    public Quaternion hiprot = Quaternion.identity;

    private Animator anim;

    private Vector2 leanSmoothRef;
    private Vector3 
        velRef,
        nrmRef,
        willRef,
        footRef;
    private float 
        footAngle,
        airtimesmooth,
        angleRef,
        landingRef,
        alignmentRef,
        hipLayerRef;
    private Transform 
        pelvis,
        head,
        animpelvis,
        animFootL,
        animFootR,
        animKneeL,
        animKneeR,
        animHandL,
        animHandR,
        animShoulderL,
        animShoulderR;

    private bool initialized = false;

    private const string
        _VEL_X = "velX",
        _VEL_Y = "velY",
        _VEL_Z = "velZ",
        _NRM_X = "nrmX",
        _NRM_Y = "nrmY",
        _NRM_Z = "nrmZ",
        _WILL_X = "willX",
        _WILL_Z = "willZ",
        _HANG_X = "hangX",
        _HANG_Z = "hangZ",
        _LEAN_X = "leanX",
        _LEAN_Z = "leanZ",
        _ANIMCYCLE = "animcycle",
        _AIRTIME = "airtime",
        _ALIGNMENT = "alignment",
        _CLIMB = "climb",
        _FOOT_ANGLE = "footAngle",
        _GROUNDED = "grounded",
        _SLIDE = "slide",
        _BSLIDE = "bslide",
        _CRAWL = "crawl",
        _FLIGHT = "flight",
        _GRAB_L = "grabL",
        _GRAB_R = "grabR",
        _CLIMBING = "climbing",
        _FOOTSLIDE = "footslide";


    private void Start()
    {
        pelvis = PlayerInfo.pelvis;
        head = PlayerInfo.head;
    }
    private void Update()
    {
        if (!initialized) return;

        transform.position += pelvis.position - animpelvis.position;
        transform.rotation = pelvis.rotation;
    }

    private void OnEnable()
    {
        PlayerInfo.OnAssignVismodel.AddListener(OnAssignVismodel);
        PlayerInfo.OnRemoveVismodel.AddListener(OnVismodelRemoved);
    }

    private void OnDisable()
    {
        PlayerInfo.OnAssignVismodel.RemoveListener(OnAssignVismodel);
        PlayerInfo.OnRemoveVismodel.RemoveListener(OnVismodelRemoved);
    }

    private void OnAssignVismodel(LucidVismodel visModel)
    {
        foreach(Transform t in GetComponentsInChildren<Transform>())
        {
            if (t != transform)
                Destroy(t.gameObject); //clear existing playermodel if applicable
        }
        GameObject newPlayerModel = Instantiate(visModel.gameObject, transform);
        LucidVismodel newVisModel = newPlayerModel.GetComponent<LucidVismodel>();
        Avatar targetAvatar = newVisModel.anim.avatar;
        newVisModel.playerMeshParent.gameObject.SetActive(false);
        Destroy(newVisModel);
        anim = newPlayerModel.GetComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        anim.avatar = targetAvatar;
        PlayerInfo.playermodelAnim = anim;
        Component c = newPlayerModel.AddComponent(typeof(AnimatorEventReciever));
        ((AnimatorEventReciever)c).target = this;

        animpelvis = anim.GetBoneTransform(HumanBodyBones.Hips);
        animFootL = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        animFootR = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        animKneeL = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        animKneeR = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        animHandL = anim.GetBoneTransform(HumanBodyBones.LeftHand);
        animHandR = anim.GetBoneTransform(HumanBodyBones.RightHand);
        animShoulderL = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        animShoulderR = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        PlayerInfo.IK_LF = IK_LF;
        PlayerInfo.IK_RF = IK_RF;
        PlayerInfo.IK_LH = IK_LH;
        PlayerInfo.IK_RH = IK_RH;
        PlayerInfo.mainCamera = fpcam;
    }

    public void OnVismodelRemoved()
    {
        PlayerInfo.animModelInitialized = false;
        initialized = false;
    }

    private void FixedUpdate()
    {
        if (anim == null) return;

        if (!initialized)
        {
            if (anim.isInitialized)
            {
                initialized = true;
                PlayerInfo.animModelInitialized = true;
                PlayerInfo.OnAnimModellInitialized.Invoke();
            }
        }
    }

    public void AnimatorMoveDelegate()
    {
        if (!initialized) return;

        CollectBoneRotations();
        hiprot = anim.GetBoneTransform(HumanBodyBones.Hips).rotation;
    }

    private void CollectBoneRotations()
    {
        foreach (HumanBodyBones hb2 in Shortcuts.hb2list)
        {
            Transform t = anim.GetBoneTransform(hb2);
            string hbstring = Shortcuts.boneNames[hb2];
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

    public void AnimatorIKDelegate(int layerIndex)
    {
        if (!initialized) return;

        bool bslide = LucidInputValueShortcuts.bslide;
        bool slide = LucidInputValueShortcuts.slide;

        Vector2 moveVector = LucidInputValueShortcuts.movement;
        Vector3 moveFlat = Vector3.zero;
        moveFlat.x = moveVector.x;
        moveFlat.z = moveVector.y;

        Vector3 localVel = CalculateLocalVelocity();
        Vector3 velflat = localVel;
        velflat.y = 0;
        Vector3 localNrm = CalculateLocalNormal();
        Vector3 willFlat = CalculateWillFlat();
        float lastalignment = CalculateAlignment();
        Vector2 lerplean = CalculateLean(moveFlat, localVel, localNrm);

        UpdateFootAngle();
        UpdateAnimatorFloats(localVel, willFlat, localNrm, lastalignment, lerplean);
        UpdateAnimatorBools();

        if (PlayerInfo.airtime > airtimesmooth)
            airtimesmooth = PlayerInfo.airtime;
        else
            airtimesmooth = Mathf.SmoothDamp(airtimesmooth, PlayerInfo.airtime, ref landingRef, landtime);

        float legweight = Mathf.Clamp01(airtimesmooth / airtimemax);
        legweight = Mathf.Clamp01(legweight + (1 - localNrm.y));
        legweight *= slide ? 0 : 1;
        legweight *= bslide ? 0 : 1;
        legweight *= PlayerInfo.flying ? 0 : 1;
        legweight *= PlayerInfo.climbing ? 0 : 1;
        anim.SetLayerWeight(1, legweight);

        float hipweight = Mathf.Clamp01((slide ? 0 : 1) * (bslide ? 0 : 1));
        hipweight *= Mathf.Clamp01(velflat.magnitude);
        hipweight *= PlayerInfo.climbing ? 0 : 1;
        hipweight *= Mathf.Clamp01(1 - Mathf.Clamp(PlayerInfo.grounddist * hipWeightReductionByHeight, 0, Mathf.Infinity));
        float currenthipweight = anim.GetLayerWeight(2);
        hipweight = Mathf.SmoothDamp(currenthipweight, hipweight, ref hipLayerRef, 0.5f);
        anim.SetLayerWeight(2, hipweight);

        Quaternion chest = anim.GetBoneTransform(HumanBodyBones.Chest).rotation;
        Quaternion localSpaceRotationNeck = Quaternion.Inverse(chest) * Quaternion.Slerp(head.rotation, chest, lerp);
        anim.SetBoneLocalRotation(HumanBodyBones.Neck, localSpaceRotationNeck);
        anim.SetBoneLocalRotation(HumanBodyBones.Head, localSpaceRotationNeck);

        UpdateFootPositions();
        UpdateHandPositions();
    }

    private float CalculateAlignment()
    {
        float lastalignment = anim.GetFloat(_ALIGNMENT);
        lastalignment = Mathf.SmoothDamp(PlayerInfo.alignment, lastalignment, ref alignmentRef, alignmentSmoothTime);
        return lastalignment;
    }

    private Vector2 CalculateLean(Vector3 moveFlat, Vector3 localVel, Vector3 localNrm)
    {
        Vector2 oldlean = Vector2.zero;
        oldlean.x = anim.GetFloat(_LEAN_X);
        oldlean.y = anim.GetFloat(_LEAN_Z);
        Vector2 lean = LeanCalc(moveFlat, localVel * 0.25f, localNrm);
        Vector2 lerplean = Vector2.SmoothDamp(oldlean, lean, ref leanSmoothRef, leansmoothtime);
        return lerplean;
    }

    private Vector3 CalculateLocalVelocity()
    {
        Vector3 localVel = PlayerInfo.pelvis.InverseTransformVector(PlayerInfo.mainBody.velocity);
        localVel *= velscale;
        Vector3 currentvellocal = Vector3.zero;
        currentvellocal.x = anim.GetFloat(_VEL_X);
        currentvellocal.y = anim.GetFloat(_VEL_Y);
        currentvellocal.z = anim.GetFloat(_VEL_Z);
        return Vector3.SmoothDamp(localVel, currentvellocal, ref velRef, velsmoothTime);
    }

    private Vector3 CalculateLocalNormal()
    {
        Vector3 localNrm = PlayerInfo.pelvis.InverseTransformVector(PlayerInfo.hipspace.up);
        Vector3 currentnrmlocal = Vector3.zero;
        currentnrmlocal.x = anim.GetFloat(_NRM_X);
        currentnrmlocal.y = anim.GetFloat(_NRM_Y);
        currentnrmlocal.z = anim.GetFloat(_NRM_Z);
        return Vector3.SmoothDamp(currentnrmlocal, localNrm, ref nrmRef, nrmSmoothTime);
    }

    private Vector3 CalculateWillFlat()
    {
        Vector2 moveVector = LucidInputValueShortcuts.movement;
        Vector3 moveFlat = Vector3.zero;
        moveFlat.x = moveVector.x;
        moveFlat.z = moveVector.y;
        Vector3 currentWill = Vector3.zero;
        currentWill.x = anim.GetFloat(_WILL_X);
        currentWill.z = anim.GetFloat(_WILL_Z);
        return Vector3.SmoothDamp(currentWill, moveFlat, ref willRef, willSmoothTime);
    }


    private void UpdateFootAngle()
    {
        footAngle = Mathf.DeltaAngle(angleRef, PlayerInfo.pelvis.eulerAngles.y);
        footAngle %= 360;
        footAngle = footAngle > 180 ? footAngle - 360 : footAngle;
        footAngle /= Mathf.Clamp(PlayerInfo.mainBody.velocity.magnitude * unrotateFeetBySpeed, 1, 100);

        if (Mathf.Abs(footAngle) > maxFootAngle)
        {
            footAngle = 0;
            angleRef = PlayerInfo.pelvis.eulerAngles.y;
            PlayerInfo.animphase += 0.5f;
            PlayerInfo.animphase %= 1;
        }
    }

    private void UpdateAnimatorFloats(Vector3 localVel, Vector3 willFlat, Vector3 localNrm, float lastalignment, Vector2 lean)
    {
        anim.SetFloat(_VEL_X, localVel.x);
        anim.SetFloat(_VEL_Y, localVel.y);
        anim.SetFloat(_VEL_Z, localVel.z);
        anim.SetFloat(_WILL_X, willFlat.x);
        anim.SetFloat(_WILL_Z, willFlat.z);
        anim.SetFloat(_NRM_X, localNrm.x);
        anim.SetFloat(_NRM_Y, localNrm.y);
        anim.SetFloat(_NRM_Z, localNrm.z);
        anim.SetFloat(_ANIMCYCLE, PlayerInfo.animphase);
        anim.SetFloat(_AIRTIME, PlayerInfo.airtime);
        anim.SetFloat(_ALIGNMENT, lastalignment);
        anim.SetFloat(_CLIMB, PlayerInfo.climbrelative.y);
        anim.SetFloat(_HANG_X, PlayerInfo.climbrelative.x);
        anim.SetFloat(_HANG_Z, PlayerInfo.climbrelative.z);
        anim.SetFloat(_LEAN_X, lean.x);
        anim.SetFloat(_LEAN_Z, lean.y);
        anim.SetFloat(_FOOT_ANGLE, footAngle / maxFootAngle);
    }

    private void UpdateAnimatorBools()
    {
        bool slide = LucidInputValueShortcuts.slide;
        bool crawl = LucidInputValueShortcuts.bslide;
        bool footslide = (PlayerInfo.alignment > footslidethreshold && PlayerInfo.mainBody.velocity.magnitude > footslidevelthreshold);

        anim.SetBool(_GROUNDED, PlayerInfo.grounded);
        anim.SetBool(_SLIDE, slide);
        anim.SetBool(_BSLIDE, crawl);
        anim.SetBool(_CRAWL, PlayerInfo.crawling);
        anim.SetBool(_FLIGHT, PlayerInfo.flying);
        anim.SetBool(_GRAB_L, PlayerInfo.grabL);
        anim.SetBool(_GRAB_R, PlayerInfo.grabR);
        anim.SetBool(_CLIMBING, PlayerInfo.climbing);
        anim.SetBool(_FOOTSLIDE, footslide);
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

        if (PlayerInfo.alignment > 0.5f && PlayerInfo.movespeed > footslidethreshold && PlayerInfo.airtime < airtimeThreshold)
            lean = -lean;

        return lean;
    }

    private void UpdateFootPositions()
    {
        Vector3 footposL = animFootL.position;
        Vector3 footposR = animFootR.position;
        Vector3 kneeposL = animKneeL.position;
        Vector3 kneeposR = animKneeR.position;

        UpdateFootPosition(true, footposL, kneeposL, PlayerInfo.legspaceL, ref LCast);
        UpdateFootPosition(false, footposR, kneeposR, PlayerInfo.legspaceR, ref RCast);

        PlayerInfo.grounded = (LCast != footposL || RCast != footposR) || PlayerInfo.pelviscollision;

        if (PlayerInfo.crawling)
            PlayerInfo.footsurface = PlayerInfo.hipspace.up;

        PlayerInfo.IK_LF.position = LCast;
        PlayerInfo.IK_RF.position = RCast;
    }

    private void UpdateFootPosition(bool isLeft, Vector3 footpos, Vector3 kneepos, Transform legspace, ref Vector3 Cast)
    {
        RaycastHit hitInfoThigh = new RaycastHit();
        RaycastHit hitInfoShin = new RaycastHit();

        bool thighCast = Physics.SphereCast(legspace.position + (legspace.up * castheight), castthickness, kneepos - legspace.position, out hitInfoThigh, Vector3.Distance(footpos, legspace.position), Shortcuts.geometryMask);
        bool shinCast = Physics.SphereCast(kneepos, castthickness, footpos - legspace.position, out hitInfoShin, Vector3.Distance(footpos, legspace.position) * groundedforgiveness, Shortcuts.geometryMask);

        Vector3 CastOld = Cast;
        if (thighCast)
        {
            Cast = hitInfoThigh.point;
            UpdateFootSpaceAndRotation(isLeft, hitInfoThigh.normal, hitInfoThigh.point);
        }
        else if (shinCast)
        {
            Cast = hitInfoShin.point;
            UpdateFootSpaceAndRotation(isLeft, hitInfoShin.normal, hitInfoShin.point);
        }
        else
            Cast = footpos;

        Cast = Vector3.SmoothDamp(Cast, CastOld, ref footRef, footSmoothTime);
    }

    private void UpdateFootSpaceAndRotation(bool isLeft, Vector3 normal, Vector3 point)
    {
        if (isLeft)
            PlayerInfo.footsurfL = normal;
        else
            PlayerInfo.footsurfR = normal;

        PlayerInfo.footsurface = normal;
        PlayerInfo.footspace.position = point + (normal * verticalFootAdjust);
        PlayerInfo.footspace.up = normal;

        Vector3 footforward = PlayerInfo.pelvis.forward;
        if (Mathf.Abs(normal.y) < 0.1f)
            footforward = Vector3.up;

        if (isLeft)
            PlayerInfo.IK_LF.rotation = Quaternion.LookRotation(footforward, normal);
        else
            PlayerInfo.IK_RF.rotation = Quaternion.LookRotation(footforward, normal);
    }

    private void UpdateHandPositions()
    {
        if (!PlayerInfo.grabL && !PlayerInfo.forceIK_LH)
            UpdateHandPosition(true, animShoulderL, animHandL);

        if (!PlayerInfo.grabR && !PlayerInfo.forceIK_RH)
            UpdateHandPosition(false, animShoulderR, animHandR);
    }

    private void UpdateHandPosition(bool isLeft, Transform shoulder, Transform hand)
    {
        RaycastHit armHitInfo;
        bool armHit = Physics.SphereCast(shoulder.position, castthickness / 2, hand.position - shoulder.position, out armHitInfo, Vector3.Distance(hand.position, shoulder.position), Shortcuts.geometryMask);

        if (armHit)
        {
            Vector3 handforward = PlayerInfo.pelvis.forward;
            if (Mathf.Abs(armHitInfo.normal.y) < 0.05f)
                handforward = Vector3.up;

            if (isLeft)
            {
                PlayerInfo.IK_LH.position = armHitInfo.point;
                PlayerInfo.IK_LH.rotation = Quaternion.LookRotation(handforward, armHitInfo.normal);
            }
            else
            {
                PlayerInfo.IK_RH.position = armHitInfo.point;
                PlayerInfo.IK_RH.rotation = Quaternion.LookRotation(handforward, armHitInfo.normal);
            }
        }
        else
        {
            if (isLeft)
                PlayerInfo.IK_LH.position = Vector3.zero;
            else
                PlayerInfo.IK_RH.position = Vector3.zero;
        }
    }

    public float SlopeLeanCalc(float slope)
    {
        if (Mathf.Abs(slope) < 0.75f) return -1.333f * slope;
        else
            return slope;
    }

    public void OnReachedGroundedState()
    {

    }
}
