using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class LucidAnimationModel : MonoBehaviour
{
    public float airtimeThreshold = 0.5f;

    [SerializeField] Transform IK_LF;
    [SerializeField] Transform IK_RF;
    [SerializeField] Transform IK_LH;
    [SerializeField] Transform IK_RH;
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
    [SerializeField] float unrotateFeetBySpeed = 1;
    [SerializeField] float maxFootAngle = 90;
    [SerializeField] float verticalFootAdjust = 0.1f;
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
    private float refAngle = 0;
    private float footAngle = 0;
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
        PlayerInfo.IK_LF = IK_LF;
        PlayerInfo.IK_RF = IK_RF;
        PlayerInfo.IK_LH = IK_LH;
        PlayerInfo.IK_RH = IK_RH;
        PlayerInfo.mainCamera = fpcam;
    }
    private void Update()
    {
        transform.position += pelvis.position - animpelvis.position;
        transform.rotation = pelvis.rotation;
        currentsway = Mathf.Sin(Time.time * swayspeed) * swaymult;
    }

    private void OnEnable()
    {
        PlayerInfo.OnAssignVismodel.AddListener(OnAssignVismodel);
    }

    private void OnDisable()
    {
        PlayerInfo.OnAssignVismodel.RemoveListener(OnAssignVismodel);
    }

    private void OnAssignVismodel(LucidVismodel visModel)
    {
        foreach(HumanBodyBones hb2 in Shortcuts.hb2list_full)
        {
            Transform tVisBone = visModel.anim.GetBoneTransform(hb2);
            Transform tAnimBone = anim.GetBoneTransform(hb2);
            Transform tVisChild = visModel.anim.GetBoneTransform(Shortcuts.PrimaryChild(hb2));
            Transform tAnimChild = anim.GetBoneTransform(Shortcuts.PrimaryChild(hb2));

            if (Vector3.Distance(tVisBone.position, tVisChild.position) < 0.001f)
                continue;

            float visBoneLength = Vector3.Distance(tVisBone.position, tVisChild.position);
            float animBoneLength = Vector3.Distance(tAnimBone.position, tAnimChild.position);

            tAnimBone.localPosition = tVisBone.localPosition;
            tAnimBone.localRotation = tVisBone.localRotation;

            float targetlength = (visBoneLength / animBoneLength);
            tAnimBone.localScale = new Vector3(1, targetlength, 1);
        }

        //anim.avatar = visModel.anim.avatar;
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

    private void OnAnimatorIK(int layerIndex)
    {
        bool crawl = LucidInputValueShortcuts.crawl;
        bool slide = LucidInputValueShortcuts.slide;
        if (slide || crawl)
            stucksliding = true;

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
            airtimesmooth = Mathf.Lerp(airtimesmooth, PlayerInfo.airtime, landspeed);

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

        bool midaircrouching = LucidInputValueShortcuts.crouch && !(PlayerInfo.grounded || slide || crawl || PlayerInfo.flying);

        float targetcrouch = midaircrouch;
        if (!midaircrouching)
            targetcrouch = 0;

        currentcrouch = Mathf.Lerp(targetcrouch, currentcrouch, midaircrouchsmoothness);

        UpdateFootPositions();
        UpdateHandPositions();
    }

    private float CalculateAlignment()
    {
        float lastalignment = anim.GetFloat("alignment");
        lastalignment = Mathf.Lerp(PlayerInfo.alignment, lastalignment, alignmentsmoothness);
        return lastalignment;
    }

    private Vector2 CalculateLean(Vector3 moveFlat, Vector3 localVel, Vector3 localNrm)
    {
        Vector2 oldlean = Vector2.zero;
        oldlean.x = anim.GetFloat("leanX");
        oldlean.y = anim.GetFloat("leanZ");
        Vector2 lean = LeanCalc(moveFlat, localVel * 0.25f, localNrm);
        Vector2 lerplean = Vector2.Lerp(oldlean, lean, leansmoothness);
        return lerplean;
    }

    private Vector3 CalculateLocalVelocity()
    {
        Vector3 localVel = PlayerInfo.hipspace.InverseTransformVector(PlayerInfo.mainBody.velocity);
        localVel *= velscale;
        Vector3 currentvellocal = new Vector3(anim.GetFloat("velX"), anim.GetFloat("velY"), anim.GetFloat("velZ"));
        return Vector3.Lerp(localVel, currentvellocal, velsmoothness);
    }

    private Vector3 CalculateLocalNormal()
    {
        Vector3 localNrm = PlayerInfo.pelvis.InverseTransformVector(PlayerInfo.hipspace.up);
        Vector3 currentnrmlocal = new Vector3(anim.GetFloat("nrmX"), anim.GetFloat("nrmY"), anim.GetFloat("nrmZ"));
        return Vector3.Lerp(currentnrmlocal, localNrm, 1 - nrmsmoothness);
    }

    private Vector3 CalculateWillFlat()
    {
        Vector2 moveVector = LucidInputActionRefs.movement.ReadValue<Vector2>();
        Vector3 moveFlat = new Vector3(moveVector.x, 0, moveVector.y);
        Vector3 currentWill = new Vector3(anim.GetFloat("willX"), 0, anim.GetFloat("willZ"));
        return Vector3.Lerp(currentWill, moveFlat, willsmoothness);
    }


    private void UpdateFootAngle()
    {
        footAngle = Mathf.DeltaAngle(refAngle, PlayerInfo.pelvis.eulerAngles.y);
        footAngle %= 360;
        footAngle = footAngle > 180 ? footAngle - 360 : footAngle;
        footAngle /= Mathf.Clamp(PlayerInfo.mainBody.velocity.magnitude * unrotateFeetBySpeed, 1, 100);

        if (Mathf.Abs(footAngle) > maxFootAngle)
        {
            footAngle = 0;
            refAngle = PlayerInfo.pelvis.eulerAngles.y;
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

        Cast = Vector3.Lerp(Cast, CastOld, footsmoothness);
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
        stucksliding = false;
    }
}
