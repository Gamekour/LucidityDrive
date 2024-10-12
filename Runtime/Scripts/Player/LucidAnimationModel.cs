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
        unrotateFeetBySpeed,
        maxFootAngle,
        verticalFootAdjust;

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
        alignmentRef;
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
        newVisModel.playerMeshParent.gameObject.SetActive(false);
        Destroy(newVisModel);
        anim = newPlayerModel.GetComponent<Animator>();
        PlayerInfo.playermodelAnim = anim;
        anim.runtimeAnimatorController = controller;
        Component c = newPlayerModel.AddComponent(typeof(AnimatorIKReciever));
        ((AnimatorIKReciever)c).target = this;

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
        if (anim != null)
        {
            if (anim.isInitialized && !PlayerInfo.animModelInitialized)
            {
                PlayerInfo.animModelInitialized = true;
                PlayerInfo.OnAnimModellInitialized.Invoke();
                initialized = true;
            }
        }
    }

    public void AnimatorMoveDelegate()
    {
        if (!initialized) return;

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

    public void AnimatorIKDelegate(int layerIndex)
    {
        if (!initialized) return;

        bool crawl = LucidInputValueShortcuts.crawl;
        bool slide = LucidInputValueShortcuts.slide;

        Vector2 moveVector = LucidInputActionRefs.movement.ReadValue<Vector2>();
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
        legweight *= crawl ? 0 : 1;
        legweight *= PlayerInfo.flying ? 0 : 1;
        legweight *= PlayerInfo.climbing ? 0 : 1;
        anim.SetLayerWeight(1, legweight);

        float hipweight = Mathf.Clamp01((slide ? 0 : 1) * (crawl ? 0 : 1));
        hipweight *= Mathf.Clamp01(velflat.magnitude);
        hipweight *= PlayerInfo.climbing ? 0 : 1;
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
        float lastalignment = anim.GetFloat("alignment");
        lastalignment = Mathf.SmoothDamp(PlayerInfo.alignment, lastalignment, ref alignmentRef, alignmentSmoothTime);
        return lastalignment;
    }

    private Vector2 CalculateLean(Vector3 moveFlat, Vector3 localVel, Vector3 localNrm)
    {
        Vector2 oldlean = Vector2.zero;
        oldlean.x = anim.GetFloat("leanX");
        oldlean.y = anim.GetFloat("leanZ");
        Vector2 lean = LeanCalc(moveFlat, localVel * 0.25f, localNrm);
        Vector2 lerplean = Vector2.SmoothDamp(oldlean, lean, ref leanSmoothRef, leansmoothtime);
        return lerplean;
    }

    private Vector3 CalculateLocalVelocity()
    {
        Vector3 localVel = PlayerInfo.hipspace.InverseTransformVector(PlayerInfo.mainBody.velocity);
        localVel *= velscale;
        Vector3 currentvellocal = new Vector3(anim.GetFloat("velX"), anim.GetFloat("velY"), anim.GetFloat("velZ"));
        return Vector3.SmoothDamp(localVel, currentvellocal, ref velRef, velsmoothTime);
    }

    private Vector3 CalculateLocalNormal()
    {
        Vector3 localNrm = PlayerInfo.pelvis.InverseTransformVector(PlayerInfo.hipspace.up);
        Vector3 currentnrmlocal = new Vector3(anim.GetFloat("nrmX"), anim.GetFloat("nrmY"), anim.GetFloat("nrmZ"));
        return Vector3.SmoothDamp(currentnrmlocal, localNrm, ref nrmRef, nrmSmoothTime);
    }

    private Vector3 CalculateWillFlat()
    {
        Vector2 moveVector = LucidInputActionRefs.movement.ReadValue<Vector2>();
        Vector3 moveFlat = new Vector3(moveVector.x, 0, moveVector.y);
        Vector3 currentWill = new Vector3(anim.GetFloat("willX"), 0, anim.GetFloat("willZ"));
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
        anim.SetFloat("leanX", lean.x);
        anim.SetFloat("leanZ", lean.y);
        anim.SetFloat("footAngle", footAngle / maxFootAngle);
    }

    private void UpdateAnimatorBools()
    {
        bool slide = LucidInputValueShortcuts.slide;
        bool crawl = LucidInputValueShortcuts.crawl;
        bool footslide = (PlayerInfo.alignment > footslidethreshold && PlayerInfo.mainBody.velocity.magnitude > footslidevelthreshold);

        anim.SetBool("grounded", PlayerInfo.grounded);
        anim.SetBool("slide", slide);
        anim.SetBool("bslide", crawl);
        anim.SetBool("crawl", PlayerInfo.crawling);
        anim.SetBool("flight", PlayerInfo.flying);
        anim.SetBool("grabL", PlayerInfo.grabL);
        anim.SetBool("grabR", PlayerInfo.grabR);
        anim.SetBool("climbing", PlayerInfo.climbing);
        anim.SetBool("footslide", footslide);
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
