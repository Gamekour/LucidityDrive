using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LucidAnimationModel : MonoBehaviour
{
    public float groundDistanceThreshold = 0.5f;
    public float camUpsideDownThreshold = -0.1f;

    public static bool[] layerOverrides = new bool[3];

    [SerializeField] RuntimeAnimatorController controller;
    [SerializeField] Camera fpcam;

    [SerializeField]
    Transform
        IK_LF,
        IK_RF,
        IK_LH,
        IK_RH;

    [SerializeField]
    float
        velSmoothTime,
        nrmSmoothTime,
        footSmoothTime,
        willSmoothTime,
        alignmentSmoothTime,
        hangSmoothTime,
        climbSmoothTime,
        stanceHeightSmoothTime,
        castThickness,
        castHeight,
        lerp,
        landTime,
        footSlideThreshold,
        footSlideVelThreshold,
        leanSmoothTime,
        unrotateFeetBySpeed,
        maxFootAngle,
        verticalFootAdjust,
        crouchTime,
        minCastDist,
        stepRate,
        scaleStepRateByVelocity,
        minStepRate,
        dampAnimPhaseByAirtime,
        wobbleScale,
        rollForceThreshold,
        leanScale,
        maxLeanAngle
        ;

    public UnityEvent onFootChanged;
    public UnityEvent<float> onGrounded;

    [HideInInspector]
    public Dictionary<string, Quaternion> boneRots = new();
    [HideInInspector]
    public Vector3 LCast;
    [HideInInspector]
    public Vector3 RCast;
    [HideInInspector]
    public Quaternion hiprot = Quaternion.identity;

    private Animator anim;

    private bool queueRoll = false;
    private Vector2 leanSmoothRef;
    private Vector3
        velRef,
        nrmRef,
        willRef,
        footRef,
        hangRef,
        leanOffset;
    private float
        footAngle,
        airtimesmooth,
        angleRef,
        landingRef,
        alignmentRef,
        hipLayerRef,
        stanceHeightRef,
        crouchRef,
        climbRef;
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
        animShoulderR,
        lastCastHitL,
        lastCastHitR;

    private bool initialized = false;

    private const string
        _VEL_X = "velX",
        _VEL_Y = "velY",
        _VEL_Z = "velZ",
        _VEL_X_N = "velXN",
        _VEL_Y_N = "velYN",
        _VEL_Z_N = "velZN",
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
        _FOOTSLIDE = "footslide",
        _CROUCH = "crouch",
        _GROUNDDIST = "groundDistance",
        _STANCEHEIGHT = "stanceHeight",
        _WOBBLE = "wobble",
        _ROLL = "roll",
        _HEAD_POLARITY_FWD = "headPolFwd",
        _HEAD_POLARITY_UP = "headPolUp",
        _LOOK_DELTA_Y = "lookDeltaY",
        _PELVIS_COLLISION = "pelvisCollision",
        _PROBE_PATTERN = "probePattern"
        ;
    private void Awake()
    {
        LucidPlayerInfo.camUpsideDownThreshold = camUpsideDownThreshold;   
    }
    private void Start()
    {
        pelvis = LucidPlayerInfo.pelvis;
        head = LucidPlayerInfo.head;
    }
    private void Update()
    {
        if (!initialized) return;

        transform.position += pelvis.position - animpelvis.position;
        transform.rotation = pelvis.rotation;
        transform.Rotate(Vector3.ClampMagnitude(leanOffset * leanScale, maxLeanAngle), Space.Self);

        float animPhase = LucidPlayerInfo.animPhase;

        Vector3 velflat = LucidPlayerInfo.mainBody.velocity;
        velflat.y = 0;

        float stepRateAdjusted = stepRate * (1 + (LucidPlayerInfo.mainBody.velocity.magnitude * scaleStepRateByVelocity));

        if (velflat.magnitude > 0.1f)
        {
            float add = Time.deltaTime * stepRateAdjusted;
            add /= 1 + (LucidPlayerInfo.airTime * dampAnimPhaseByAirtime);
            animPhase += add;
            animPhase %= 1;
        }

        bool isRight = animPhase > 0.5f;
        bool wasRight = LucidPlayerInfo.animPhase > 0.5f;
        if (wasRight != isRight && LucidPlayerInfo.grounded && !LucidPlayerInfo.slidingBack && !LucidPlayerInfo.slidingForward)
            onFootChanged.Invoke();

        LucidPlayerInfo.animPhase = animPhase;

        float stepphase = 0.5f - animPhase;
        stepphase = Mathf.Abs(stepphase) * 2;
        LucidPlayerInfo.stepPhase = stepphase;
    }

    private void OnEnable()
    {
        LucidPlayerInfo.OnAssignVismodel.AddListener(OnAssignVismodel);
        LucidPlayerInfo.OnRemoveVismodel.AddListener(OnVismodelRemoved);
    }

    private void OnDisable()
    {
        LucidPlayerInfo.OnAssignVismodel.RemoveListener(OnAssignVismodel);
        LucidPlayerInfo.OnRemoveVismodel.RemoveListener(OnVismodelRemoved);
    }

    private void OnAssignVismodel(LucidVismodel visModel)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>())
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
        LucidPlayerInfo.animationModel = anim;
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
        LucidPlayerInfo.IK_LF = IK_LF;
        LucidPlayerInfo.IK_RF = IK_RF;
        LucidPlayerInfo.IK_LH = IK_LH;
        LucidPlayerInfo.IK_RH = IK_RH;
    }

    public void OnVismodelRemoved()
    {
        LucidPlayerInfo.animModelInitialized = false;
        initialized = false;
    }

    private void FixedUpdate()
    {
        if (anim == null || LucidPlayerInfo.vismodelRef == null) return;

        if (!initialized)
        {
            if (anim.isInitialized)
            {
                initialized = true;
                LucidPlayerInfo.animModelInitialized = true;
                LucidPlayerInfo.OnAnimModellInitialized.Invoke();
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
        foreach (HumanBodyBones hb2 in LucidShortcuts.hb2list)
        {
            Transform t = anim.GetBoneTransform(hb2);
            string hbstring = LucidShortcuts.boneNames[hb2];

            if (t == null) continue;

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
        bool crouch = LucidInputValueShortcuts.crouch;

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
        Vector2 lerplean = CalculateLean(localVel, localNrm);
        leanOffset = new Vector3(lerplean.y, 0, -lerplean.x);
        Vector3 hang = CalculateHang();

        float currentClimb = anim.GetFloat(_CLIMB);
        float targetCimb = LucidPlayerInfo.climbRelative.y;
        hang.y = Mathf.SmoothDamp(currentClimb, targetCimb, ref climbRef, climbSmoothTime);

        float stanceHeight = CalculateStanceHeight();

        UpdateFootAngle();
        UpdateAnimatorFloats(localVel, willFlat, localNrm, lastalignment, lerplean, hang, stanceHeight);
        UpdateAnimatorBools();
        if (queueRoll)
        {
            anim.SetTrigger(_ROLL);
            queueRoll = false;
        }

        if (LucidPlayerInfo.airTime > airtimesmooth)
            airtimesmooth = LucidPlayerInfo.airTime;
        else
            airtimesmooth = Mathf.SmoothDamp(airtimesmooth, LucidPlayerInfo.airTime, ref landingRef, landTime);

        Quaternion chest = anim.GetBoneTransform(HumanBodyBones.Chest).rotation;
        Quaternion localSpaceRotationNeck = Quaternion.Inverse(chest) * Quaternion.Slerp(head.rotation, chest, lerp);
        anim.SetBoneLocalRotation(HumanBodyBones.Neck, localSpaceRotationNeck);
        anim.SetBoneLocalRotation(HumanBodyBones.Head, localSpaceRotationNeck);

        UpdateFootPositions();
        UpdateHandPositions();
    }

    private float CalculateStanceHeight()
    {
        float stanceHeight = LucidPlayerInfo.stanceHeight;
        float currentStanceHeight = anim.GetFloat(_STANCEHEIGHT);
        stanceHeight = Mathf.SmoothDamp(currentStanceHeight, stanceHeight, ref stanceHeightRef, stanceHeightSmoothTime);

        if (LucidPlayerInfo.head.up.y > 0)
        {
            CapsuleCollider hipColl = LucidPlayerInfo.pelvisColl;
            Vector3 point1 = hipColl.transform.position + (hipColl.transform.up * (hipColl.height * 0.5f - hipColl.radius));
            Vector3 point2 = hipColl.transform.position - (hipColl.transform.up * (hipColl.height * 0.5f - hipColl.radius));
            bool upHit = Physics.CapsuleCast(point1, point2, hipColl.radius - 0.1f, Vector3.up, out RaycastHit hitInfoUp, 100, LucidShortcuts.geometryMask);
            bool downHit = Physics.CapsuleCast(point1, point2, hipColl.radius - 0.1f, Vector3.down, out RaycastHit hitInfoDown, 100, LucidShortcuts.geometryMask);

            if (upHit)
            {
                float totalspace = hitInfoUp.distance + LucidPlayerInfo.calfLength + LucidPlayerInfo.thighLength;
                if (downHit)
                    totalspace = hitInfoUp.point.y - hitInfoDown.point.y;
                float heightratio = Mathf.Clamp01(totalspace / LucidPlayerInfo.vismodelRef.stanceHeightFactor);
                stanceHeight = Mathf.Clamp(stanceHeight, 0, heightratio);
            }
        }

        return stanceHeight;
    }

    private float CalculateAlignment()
    {
        float lastalignment = anim.GetFloat(_ALIGNMENT);
        lastalignment = Mathf.SmoothDamp(lastalignment, LucidPlayerInfo.alignment, ref alignmentRef, alignmentSmoothTime);
        return lastalignment;
    }

    private Vector2 CalculateLean(Vector3 localVel, Vector3 localNrm)
    {
        Vector2 oldlean = Vector2.zero;
        oldlean.x = anim.GetFloat(_LEAN_X);
        oldlean.y = anim.GetFloat(_LEAN_Z);
        Vector2 lean = LeanCalc(localVel * 0.25f, localNrm);
        Vector2 lerplean = Vector2.SmoothDamp(oldlean, lean, ref leanSmoothRef, leanSmoothTime);
        return lerplean;
    }

    private Vector3 CalculateLocalVelocity()
    {
        Vector3 localVel = LucidPlayerInfo.pelvis.InverseTransformVector(LucidPlayerInfo.mainBody.velocity);
        Vector3 currentvellocal = Vector3.zero;
        currentvellocal.x = anim.GetFloat(_VEL_X);
        currentvellocal.y = anim.GetFloat(_VEL_Y);
        currentvellocal.z = anim.GetFloat(_VEL_Z);
        return Vector3.SmoothDamp(localVel, currentvellocal, ref velRef, velSmoothTime);
    }

    private Vector3 CalculateLocalNormal()
    {
        Vector3 localNrm = LucidPlayerInfo.pelvis.InverseTransformVector(LucidPlayerInfo.hipspace.up);
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

    private Vector3 CalculateHang()
    {
        Vector3 currentClimbRelative = Vector3.zero;
        currentClimbRelative.x = anim.GetFloat(_HANG_X);
        currentClimbRelative.z = anim.GetFloat(_HANG_Z);
        Vector3 climbRelative = LucidPlayerInfo.climbRelative;
        return Vector3.SmoothDamp(currentClimbRelative, climbRelative, ref hangRef, hangSmoothTime);
    }


    private void UpdateFootAngle()
    {
        footAngle = Mathf.DeltaAngle(angleRef, LucidPlayerInfo.pelvis.eulerAngles.y);
        footAngle %= 360;
        footAngle = footAngle > 180 ? footAngle - 360 : footAngle;
        footAngle /= Mathf.Clamp(LucidPlayerInfo.mainBody.velocity.magnitude * unrotateFeetBySpeed, 1, 100);

        if (Mathf.Abs(footAngle) > maxFootAngle)
        {
            footAngle = 0;
            angleRef = LucidPlayerInfo.pelvis.eulerAngles.y;
            LucidPlayerInfo.animPhase += 0.5f;
            LucidPlayerInfo.animPhase %= 1;
        }
    }

    private void UpdateAnimatorFloats(Vector3 localVel, Vector3 willFlat, Vector3 localNrm, float lastalignment, Vector2 lean, Vector3 hang, float smoothedStanceHeight)
    {
        float currentcrouch = anim.GetFloat(_CROUCH);
        float crouch = LucidInputValueShortcuts.crouch ? 1 : 0;
        crouch = Mathf.SmoothDamp(currentcrouch, crouch, ref crouchRef, crouchTime);
        Vector3 velN = localVel.normalized;
        float trueHeadX = Vector3.SignedAngle(pelvis.forward, head.forward, pelvis.right);

        anim.SetFloat(_VEL_X, localVel.x);
        anim.SetFloat(_VEL_Y, localVel.y);
        anim.SetFloat(_VEL_Z, localVel.z);
        anim.SetFloat(_VEL_X_N, velN.x);
        anim.SetFloat(_VEL_Y_N, velN.y);
        anim.SetFloat(_VEL_Z_N, velN.z);
        anim.SetFloat(_WILL_X, willFlat.x);
        anim.SetFloat(_WILL_Z, willFlat.z);
        anim.SetFloat(_NRM_X, localNrm.x);
        anim.SetFloat(_NRM_Y, localNrm.y);
        anim.SetFloat(_NRM_Z, localNrm.z);
        anim.SetFloat(_ANIMCYCLE, LucidPlayerInfo.animPhase);
        anim.SetFloat(_AIRTIME, LucidPlayerInfo.airTime);
        anim.SetFloat(_ALIGNMENT, 1 - lastalignment);
        anim.SetFloat(_CLIMB, hang.y);
        anim.SetFloat(_HANG_X, hang.x);
        anim.SetFloat(_HANG_Z, hang.z);
        anim.SetFloat(_LEAN_X, lean.x);
        anim.SetFloat(_LEAN_Z, lean.y);
        anim.SetFloat(_FOOT_ANGLE, footAngle / maxFootAngle);
        anim.SetFloat(_CROUCH, crouch);
        anim.SetFloat(_STANCEHEIGHT, smoothedStanceHeight);
        anim.SetFloat(_GROUNDDIST, LucidPlayerInfo.groundDistance);
        anim.SetFloat(_WOBBLE, 1 + (Mathf.Abs(LucidPlayerInfo.mainBody.velocity.magnitude) * wobbleScale));
        anim.SetFloat(_LOOK_DELTA_Y, LucidInputValueShortcuts.headLook.y);
        anim.SetInteger(_PROBE_PATTERN, LucidPlayerInfo.probePattern);
    }

    private void UpdateAnimatorBools()
    {
        bool slide = LucidPlayerInfo.slidingBack;
        bool crawl = LucidPlayerInfo.slidingForward;
        bool footslide = (LucidPlayerInfo.alignment > footSlideThreshold && LucidPlayerInfo.mainBody.velocity.magnitude > footSlideVelThreshold);

        anim.SetBool(_GROUNDED, LucidPlayerInfo.grounded);
        anim.SetBool(_SLIDE, slide);
        anim.SetBool(_BSLIDE, crawl);
        anim.SetBool(_CRAWL, LucidPlayerInfo.crawling);
        anim.SetBool(_FLIGHT, LucidPlayerInfo.flying);
        anim.SetBool(_GRAB_L, LucidPlayerInfo.climbL);
        anim.SetBool(_GRAB_R, LucidPlayerInfo.climbR);
        anim.SetBool(_CLIMBING, LucidPlayerInfo.climbing);
        anim.SetBool(_FOOTSLIDE, footslide);
        anim.SetBool(_HEAD_POLARITY_FWD, LucidPlayerInfo.head.forward.y >= camUpsideDownThreshold);
        anim.SetBool(_HEAD_POLARITY_UP, LucidPlayerInfo.head.up.y >= camUpsideDownThreshold);
        anim.SetBool(_PELVIS_COLLISION, LucidPlayerInfo.pelvisCollision);
    }

    private Vector2 LeanCalc(Vector3 localvel, Vector3 localnrm, float k1 = 0.3f, float k2 = 0.7f)
    {
        Vector2 accel = Vector2.zero;
        accel.x = localvel.x;
        accel.y = localvel.z;
        Vector2 slope = Vector2.zero;
        slope.x = localnrm.x;
        slope.y = localnrm.z;
        if (LucidPlayerInfo.alignment > 0.5f && LucidPlayerInfo.groundDistance < groundDistanceThreshold)
            accel = -accel;
        Vector2 lean = Vector2.zero;
        lean.x = (accel.x * k1) + (slope.x * k2);
        lean.y = (accel.y * k1) + (slope.y * k2);

        return lean;
    }

    private void UpdateFootPositions()
    {
        Vector3 footposL = animFootL.position;
        Vector3 footposR = animFootR.position;
        Vector3 kneeposL = animKneeL.position;
        Vector3 kneeposR = animKneeR.position;

        bool hitL = UpdateFootPosition(true, footposL, kneeposL, LucidPlayerInfo.legspaceL, ref LCast);
        bool hitR = UpdateFootPosition(false, footposR, kneeposR, LucidPlayerInfo.legspaceR, ref RCast);

        bool prev_grounded = LucidPlayerInfo.grounded;

        LucidPlayerInfo.grounded = hitL || hitR;

        if (!prev_grounded && LucidPlayerInfo.grounded)
        {
            Vector3 rel_force = LucidPlayerInfo.footspace.InverseTransformVector(LucidPlayerInfo.mainBody.velocity);
            LucidPlayerInfo.lastLandingForce = rel_force.y;
            onGrounded.Invoke(LucidPlayerInfo.lastLandingForce);
            if (LucidPlayerInfo.lastLandingForce < -rollForceThreshold)
                queueRoll = true;
        }

        if (LucidPlayerInfo.crawling)
            LucidPlayerInfo.footSurface = LucidPlayerInfo.hipspace.up;

        LucidPlayerInfo.IK_LF.position = LCast;
        LucidPlayerInfo.IK_RF.position = RCast;
    }

    private bool UpdateFootPosition(bool isLeft, Vector3 footpos, Vector3 kneepos, Transform legspace, ref Vector3 Cast)
    {
        Vector3 thighOrigin = legspace.position + (legspace.up * castHeight);
        Debug.DrawLine(thighOrigin, thighOrigin + ((kneepos - thighOrigin).normalized * Vector3.Distance(thighOrigin, kneepos)), Color.magenta);
        Debug.DrawLine(kneepos, kneepos + ((footpos - legspace.position).normalized * (Vector3.Distance(kneepos, footpos))), Color.magenta);
        bool thighCast = Physics.SphereCast(thighOrigin, castThickness, (kneepos - thighOrigin).normalized, out RaycastHit hitInfoThigh, Vector3.Distance(thighOrigin, kneepos), LucidShortcuts.geometryMask);
        bool shinCast = Physics.SphereCast(kneepos, castThickness, (footpos - legspace.position).normalized, out RaycastHit hitInfoShin, Vector3.Distance(kneepos, footpos), LucidShortcuts.geometryMask);
        LucidPlayerInfo.thighLength = Vector3.Distance(thighOrigin, kneepos);
        LucidPlayerInfo.calfLength = Vector3.Distance(kneepos, footpos);
        LucidPlayerInfo.totalLegLength = LucidPlayerInfo.thighLength + LucidPlayerInfo.calfLength - castThickness;

        Vector3 CastOld = Cast;
        if (thighCast)
        {
            Cast = hitInfoThigh.point;
            UpdateFootSpaceAndRotation(isLeft, hitInfoThigh.normal, hitInfoThigh.point);

            Transform lastCastHit = isLeft ? lastCastHitL : lastCastHitR;
            if (hitInfoThigh.transform != lastCastHit)
            {
                Rigidbody rb = hitInfoThigh.transform.GetComponent<Rigidbody>();
                bool rbvalid = rb != null;
                if (isLeft)
                {
                    if (rbvalid)
                        LucidPlayerInfo.connectedRB_LF = rb;
                    else
                        LucidPlayerInfo.connectedRB_LF = null;
                    lastCastHitL = hitInfoThigh.transform;
                }
                else
                {
                    if (rbvalid)
                        LucidPlayerInfo.connectedRB_RF = rb;
                    else
                        LucidPlayerInfo.connectedRB_RF = null;
                    lastCastHitR = hitInfoThigh.transform;
                }
            }
        }
        else if (shinCast)
        {
            Cast = hitInfoShin.point;
            UpdateFootSpaceAndRotation(isLeft, hitInfoShin.normal, hitInfoShin.point);

            bool lastCastHit = isLeft ? lastCastHitL : lastCastHitR;
            if (hitInfoShin.transform != lastCastHit)
            {
                Rigidbody rb = hitInfoShin.transform.GetComponent<Rigidbody>();
                bool rbvalid = rb != null;
                if (isLeft)
                {
                    if (rbvalid)
                        LucidPlayerInfo.connectedRB_LF = rb;
                    lastCastHitL = hitInfoShin.transform;
                }
                else
                {
                    if (rbvalid)
                        LucidPlayerInfo.connectedRB_RF = rb;
                    lastCastHitR = hitInfoShin.transform;
                }
            }
        }
        else
        {
            Cast = footpos;
            if (isLeft)
            {
                LucidPlayerInfo.connectedRB_LF = null;
                LucidPlayerInfo.IK_LF.rotation = Quaternion.LookRotation(LucidPlayerInfo.pelvis.forward);
                lastCastHitL = null;
            }
            else
            {
                LucidPlayerInfo.connectedRB_RF = null;
                LucidPlayerInfo.IK_RF.rotation = Quaternion.LookRotation(LucidPlayerInfo.pelvis.forward);
                lastCastHitR = null;
            }
        }

        if (Vector3.Distance(thighOrigin, Cast) < minCastDist)
            Cast = footpos;

        Cast = Vector3.SmoothDamp(Cast, CastOld, ref footRef, footSmoothTime);


        return thighCast || shinCast;
    }

    private void UpdateFootSpaceAndRotation(bool isLeft, Vector3 normal, Vector3 point)
    {
        bool highcheck = Physics.SphereCast(point + Vector3.up * 0.2f, 0.1f, Vector3.down, out RaycastHit hitInfo, 0.2f, LucidShortcuts.geometryMask);
        if (highcheck)
        {
            if (hitInfo.normal.y > normal.y)
            {
                normal = hitInfo.normal;
            }
        }

        if (isLeft)
            LucidPlayerInfo.footSurfaceL = normal;
        else
            LucidPlayerInfo.footSurfaceR = normal;

        LucidPlayerInfo.footSurface = normal;
        LucidPlayerInfo.footspace.position = point + (normal * verticalFootAdjust);
        LucidPlayerInfo.footspace.up = normal;

        Vector3 footforward = LucidPlayerInfo.pelvis.forward;
        if (Mathf.Abs(normal.y) < 0.1f)
            footforward = Vector3.up;

        if (isLeft)
            LucidPlayerInfo.IK_LF.rotation = Quaternion.LookRotation(footforward, normal);
        else
            LucidPlayerInfo.IK_RF.rotation = Quaternion.LookRotation(footforward, normal);
    }

    private void UpdateHandPositions()
    {
        if (!LucidPlayerInfo.grabL)
        {
            LucidPlayerInfo.handTargetL.SetPositionAndRotation(animHandL.position, animHandL.rotation);
            UpdateHandPosition(true, animShoulderL, LucidPlayerInfo.handTargetL);
        }
        if (!LucidPlayerInfo.grabR)
        {
            LucidPlayerInfo.handTargetR.SetPositionAndRotation(animHandR.position, animHandR.rotation);
            UpdateHandPosition(false, animShoulderR, LucidPlayerInfo.handTargetR);
        }
    }

    private void UpdateHandPosition(bool isLeft, Transform shoulder, Transform hand)
    {
        bool armHit = Physics.SphereCast(shoulder.position, castThickness / 2, hand.position - shoulder.position, out RaycastHit armHitInfo, Vector3.Distance(hand.position, shoulder.position), LucidShortcuts.geometryMask);

        if (isLeft)
            LucidPlayerInfo.handCollisionL = armHit;
        else
            LucidPlayerInfo.handCollisionR = armHit;

        if (armHit)
        {
            Vector3 handforward = LucidPlayerInfo.pelvis.forward;
            if (Mathf.Abs(armHitInfo.normal.y) < 0.05f)
                handforward = Vector3.up;

            if (isLeft)
            {
                LucidPlayerInfo.IK_LH.SetPositionAndRotation(armHitInfo.point, Quaternion.LookRotation(handforward, armHitInfo.normal));
            }
            else
            {
                LucidPlayerInfo.IK_RH.SetPositionAndRotation(armHitInfo.point, Quaternion.LookRotation(handforward, armHitInfo.normal));
            }
        }
        else
        {
            if (isLeft)
                LucidPlayerInfo.IK_LH.position = Vector3.zero;
            else
                LucidPlayerInfo.IK_RH.position = Vector3.zero;
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
