using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace LucidityDrive
{
    public class LucidAnimationModel : MonoBehaviour
    {
        public static LucidAnimationModel instance;

        public AnimationSettings defaultAnimationSettings;

        private AnimationSettings m_activeAnimationSettings;
        public AnimationSettings ActiveAnimationSettings
        {
            get { return m_activeAnimationSettings; }
            set { m_activeAnimationSettings = value; CopyValues(); } //copy values any time the movement settings are changed
        }

        [SerializeField] RuntimeAnimatorController controller;
        [SerializeField] Camera fpcam;

        [SerializeField]
        Transform
            IK_LF,
            IK_RF,
            IK_LH,
            IK_RH;

        private float
            groundDistanceThresholdScale,
            camUpsideDownThreshold,
            velSmoothTime,
            footSmoothTime,
            alignmentSmoothTime,
            hangSmoothTime,
            climbSmoothTime,
            leanSmoothTime,
            flipSmoothTime,
            stanceHeightSmoothTime,
            legCastThickness,
            legCastStartHeightOffset,
            verticalFootAdjust,
            minCastDist,
            stepRate,
            scaleStepRateByVelocity,
            wobbleScale,
            hardLandingForce,
            leanScale,
            maxLeanAngle,
            highSlopeThreshold,
            velNFallback
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
            footRef,
            hangRef,
            leanOffset
            ;
        private float
            alignmentRef,
            stanceHeightRef,
            climbRef,
            flipRef,
            oldFlip
            ;
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
            lastCastHitR
            ;

        private bool initialized = false;

        private const string
            _VEL_X = "velX",
            _VEL_Z = "velZ",
            _VEL_X_N = "velXN",
            _VEL_Y_N = "velYN",
            _VEL_Z_N = "velZN",
            _HANG_X = "hangX",
            _HANG_Z = "hangZ",
            _LEAN_X = "leanX",
            _LEAN_Z = "leanZ",
            _ANIMCYCLE = "animcycle",
            _ALIGNMENT = "alignment",
            _CLIMB = "climb",
            _GROUNDED = "grounded",
            _FLIGHT = "flight",
            _GRAB_L = "grabL",
            _GRAB_R = "grabR",
            _CLIMBING = "climbing",
            _STANCEHEIGHT = "stanceHeight",
            _WOBBLE = "wobble",
            _HARD_LAND = "hardLanding",
            _PELVIS_COLLISION = "pelvisCollision",
            _PROBE_PATTERN = "probePattern",
            _SWINGING = "swinging",
            _HEAD_UP_Y = "headUpY",
            _HEAD_FWD_Y = "headForwardY"
            ;
        private void Awake()
        {
            instance = this;
            LucidPlayerInfo.camUpsideDownThreshold = camUpsideDownThreshold;
            ActiveAnimationSettings = defaultAnimationSettings;
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
            float targetFlip = 0;
            if (LucidPlayerInfo.head.up.y < camUpsideDownThreshold)
                targetFlip = Vector3.SignedAngle(LucidPlayerInfo.pelvis.forward, LucidPlayerInfo.head.forward, LucidPlayerInfo.pelvis.right);
            float flipamount = Mathf.SmoothDampAngle(oldFlip, targetFlip, ref flipRef, flipSmoothTime);
            if (LucidPlayerInfo.groundDistance < LucidPlayerInfo.totalLegLength * groundDistanceThresholdScale)
                flipamount = 0;
            transform.Rotate(Vector3.ClampMagnitude(leanOffset * leanScale, maxLeanAngle) + (Vector3.right * flipamount), Space.Self);
            oldFlip = flipamount;

            float animPhase = LucidPlayerInfo.animPhase;

            Vector3 velflat = LucidPlayerInfo.mainBody.velocity;
            velflat.y = 0;

            float stepRateAdjusted = stepRate * (1 + (LucidPlayerInfo.mainBody.velocity.magnitude * scaleStepRateByVelocity));

            if (velflat.magnitude > 0.1f)
            {
                float add = Time.deltaTime * stepRateAdjusted;
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

        public void CopyValues()
        {
            Type TAnimSettings = typeof(AnimationSettings);
            Type TAnimModel = typeof(LucidAnimationModel);
            FieldInfo[] fields = TAnimSettings.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var field in fields)
            {
                // Find the corresponding field in the destination object
                FieldInfo destField = TAnimModel.GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                if (destField != null && destField.FieldType == field.FieldType)
                {
                    // Copy the value from the source field to the destination field
                    object value = field.GetValue(ActiveAnimationSettings);
                    destField.SetValue(this, value);
                }
            }
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
            foreach(Renderer r in newVisModel.gameObject.GetComponentsInChildren<Renderer>())
            {
                Destroy(r.gameObject);
            }
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

            Vector2 moveVector = LucidInputValueShortcuts.movement;
            Vector3 moveFlat = Vector3.zero;
            moveFlat.x = moveVector.x;
            moveFlat.z = moveVector.y;

            Vector3 localVel = CalculateLocalVelocity();
            Vector3 velflat = localVel;
            velflat.y = 0;
            float alignment = CalculateAlignment();
            Vector3 localNrm = LucidPlayerInfo.pelvis.InverseTransformDirection(LucidPlayerInfo.hipspace.up);
            Vector2 lerplean = CalculateLean(localVel, localNrm);
            leanOffset = new Vector3(lerplean.y, 0, -lerplean.x);
            Vector3 hang = CalculateHang();

            float currentClimb = anim.GetFloat(_CLIMB);
            float targetCimb = LucidPlayerInfo.climbRelative.y;
            hang.y = Mathf.SmoothDamp(currentClimb, targetCimb, ref climbRef, climbSmoothTime);

            float stanceHeight = CalculateStanceHeight();

            UpdateAnimatorFloats(localVel, alignment, lerplean, hang, stanceHeight);
            UpdateAnimatorBools();
            if (queueRoll)
            {
                anim.SetTrigger(_HARD_LAND);
                queueRoll = false;
            }

            Quaternion chest = anim.GetBoneTransform(HumanBodyBones.Chest).rotation;
            Quaternion localSpaceRotationNeck = Quaternion.Inverse(chest) * Quaternion.Slerp(head.rotation, chest, 0.5f);
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

            if (LucidPlayerInfo.head.up.y > camUpsideDownThreshold)
            {
                CapsuleCollider hipColl = LucidPlayerInfo.pelvisColl;
                Vector3 point1 = hipColl.transform.position + (hipColl.transform.up * (hipColl.height * 0.5f - hipColl.radius));
                Vector3 point2 = hipColl.transform.position - (hipColl.transform.up * (hipColl.height * 0.5f - hipColl.radius));
                bool upHit = Physics.CapsuleCast(point1, point2, hipColl.radius - 0.1f, Vector3.up, out RaycastHit hitInfoUp, 100, LucidShortcuts.geometryMask);
                bool downHit = Physics.CapsuleCast(point1, point2, hipColl.radius - 0.1f, Vector3.down, out RaycastHit hitInfoDown, 100, LucidShortcuts.geometryMask);

                if (upHit)
                {
                    float totalspace = hitInfoUp.distance + LucidPlayerInfo.totalLegLength + 0.1f;
                    if (downHit)
                        totalspace = hitInfoUp.point.y - hitInfoDown.point.y;
                    float heightratio = Mathf.Clamp01(totalspace / LucidPlayerInfo.vismodelRef.stanceHeightFactor);
                    LucidPlayerInfo.maxStanceHeight = heightratio;
                }
                else
                    LucidPlayerInfo.maxStanceHeight = 1;
            }

            return stanceHeight;
        }

        private float CalculateAlignment()
        {
            float lastalignment = anim.GetFloat(_ALIGNMENT);
            float alignmentSmooth = Mathf.SmoothDamp(lastalignment, LucidPlayerInfo.alignment, ref alignmentRef, alignmentSmoothTime);
            return alignmentSmooth;
        }

        private Vector2 CalculateLean(Vector3 localVel, Vector3 localNrm)
        {
            Vector2 oldlean = Vector2.zero;
            oldlean.x = -leanOffset.z;
            oldlean.y = leanOffset.x;
            Vector2 lean = LeanCalc(localVel * 0.25f, localNrm);
            Vector2 lerplean = Vector2.SmoothDamp(oldlean, lean, ref leanSmoothRef, leanSmoothTime);
            return lerplean;
        }

        private Vector3 CalculateLocalVelocity()
        {
            Vector3 localVel = LucidPlayerInfo.pelvis.InverseTransformVector(LucidPlayerInfo.mainBody.velocity);
            Vector3 currentvellocal = Vector3.zero;
            currentvellocal.x = anim.GetFloat(_VEL_X);
            currentvellocal.z = anim.GetFloat(_VEL_Z);
            return Vector3.SmoothDamp(localVel, currentvellocal, ref velRef, velSmoothTime);
        }

        private Vector3 CalculateHang()
        {
            Vector3 currentClimbRelative = Vector3.zero;
            currentClimbRelative.x = anim.GetFloat(_HANG_X);
            currentClimbRelative.z = anim.GetFloat(_HANG_Z);
            Vector3 climbRelative = LucidPlayerInfo.climbRelative;
            return Vector3.SmoothDamp(currentClimbRelative, climbRelative, ref hangRef, hangSmoothTime);
        }

        private void UpdateAnimatorFloats(Vector3 localVel, float alignment, Vector2 lean, Vector3 hang, float smoothedStanceHeight)
        {
            Vector3 velN = localVel.normalized;
            if (localVel.magnitude < 0.1f)
                velN = Vector3.zero;
            if (localVel.x < 0.1f && localVel.z < 0.1f)
                velN.y = localVel.y * velNFallback;

            anim.SetFloat(_VEL_X, localVel.x);
            anim.SetFloat(_VEL_Z, localVel.z);
            anim.SetFloat(_VEL_X_N, velN.x);
            anim.SetFloat(_VEL_Y_N, velN.y);
            anim.SetFloat(_VEL_Z_N, velN.z);
            anim.SetFloat(_ANIMCYCLE, LucidPlayerInfo.animPhase);
            anim.SetFloat(_ALIGNMENT, alignment);
            anim.SetFloat(_CLIMB, hang.y);
            anim.SetFloat(_HANG_X, hang.x);
            anim.SetFloat(_HANG_Z, hang.z);
            anim.SetFloat(_LEAN_X, lean.x);
            anim.SetFloat(_LEAN_Z, lean.y);
            anim.SetFloat(_STANCEHEIGHT, smoothedStanceHeight);
            anim.SetFloat(_WOBBLE, 1 + (Mathf.Abs(LucidPlayerInfo.mainBody.velocity.magnitude) * wobbleScale));
            anim.SetFloat(_HEAD_UP_Y, LucidPlayerInfo.head.up.y);
            anim.SetFloat(_HEAD_FWD_Y, LucidPlayerInfo.head.forward.y);
            anim.SetInteger(_PROBE_PATTERN, LucidPlayerInfo.probePattern);
        }

        private void UpdateAnimatorBools()
        {
            bool slide = LucidPlayerInfo.slidingBack;

            anim.SetBool(_GROUNDED, LucidPlayerInfo.groundDistance < LucidPlayerInfo.totalLegLength * groundDistanceThresholdScale);
            anim.SetBool(_FLIGHT, LucidPlayerInfo.flying);
            anim.SetBool(_GRAB_L, LucidPlayerInfo.climbL);
            anim.SetBool(_GRAB_R, LucidPlayerInfo.climbR);
            anim.SetBool(_CLIMBING, LucidPlayerInfo.climbing);
            anim.SetBool(_PELVIS_COLLISION, LucidPlayerInfo.pelvisCollision);
            anim.SetBool(_SWINGING, LucidPlayerInfo.swinging);
        }

        private Vector2 LeanCalc(Vector3 localvel, Vector3 localnrm, float k1 = 0.3f, float k2 = 0.7f)
        {
            Vector2 accel = Vector2.zero;
            accel.x = localvel.x;
            accel.y = localvel.z;
            Vector2 slope = Vector2.zero;
            slope.x = localnrm.x;
            slope.y = localnrm.z;
            if (LucidPlayerInfo.probePattern == 1)
                slope = Vector2.zero;
            if (LucidPlayerInfo.stanceHeight < 0.11f)
                accel = Vector2.zero;
            else if (LucidPlayerInfo.alignment < 0.5f && LucidPlayerInfo.groundDistance < LucidPlayerInfo.totalLegLength * groundDistanceThresholdScale)
                accel = -accel * 2;
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
                LucidPlayerInfo.lastLandingForce = LucidPlayerInfo.mainBody.velocity.y;
                onGrounded.Invoke(LucidPlayerInfo.lastLandingForce);
                if (LucidPlayerInfo.lastLandingForce < -hardLandingForce)
                    queueRoll = true;
            }

            if (LucidPlayerInfo.crawling)
                LucidPlayerInfo.footSurface = LucidPlayerInfo.hipspace.up;

            LucidPlayerInfo.IK_LF.position = LCast;
            LucidPlayerInfo.IK_RF.position = RCast;
        }

        private bool UpdateFootPosition(bool isLeft, Vector3 footpos, Vector3 kneepos, Transform legspace, ref Vector3 Cast)
        {
            Vector3 thighOrigin = legspace.position + (legspace.up * legCastStartHeightOffset);
            Debug.DrawLine(thighOrigin, thighOrigin + ((kneepos - thighOrigin).normalized * Vector3.Distance(thighOrigin, kneepos)), Color.magenta);
            Debug.DrawLine(kneepos, kneepos + ((footpos - legspace.position).normalized * (Vector3.Distance(kneepos, footpos))), Color.magenta);
            bool thighCast = Physics.SphereCast(thighOrigin, legCastThickness, (kneepos - thighOrigin).normalized, out RaycastHit hitInfoThigh, Vector3.Distance(thighOrigin, kneepos), LucidShortcuts.geometryMask);
            bool shinCast = Physics.SphereCast(kneepos, legCastThickness, (footpos - legspace.position).normalized, out RaycastHit hitInfoShin, Vector3.Distance(kneepos, footpos), LucidShortcuts.geometryMask);
            LucidPlayerInfo.thighLength = Vector3.Distance(thighOrigin, kneepos);
            LucidPlayerInfo.calfLength = Vector3.Distance(kneepos, footpos);
            LucidPlayerInfo.totalLegLength = LucidPlayerInfo.thighLength + LucidPlayerInfo.calfLength - legCastThickness;

            Vector3 CastOld = Cast;
            if (thighCast)
            {
                Cast = hitInfoThigh.point;
                UpdateFootSpaceAndRotation(isLeft, hitInfoThigh.normal, hitInfoThigh.point);

                ref Transform lastCastHit = ref lastCastHitR;
                if (isLeft)
                    lastCastHit = ref lastCastHitR;
                ref Rigidbody connectedRB = ref LucidPlayerInfo.connectedRB_RF;
                if (isLeft)
                    connectedRB = ref LucidPlayerInfo.connectedRB_LF;
                ref Collider connectedColl = ref LucidPlayerInfo.connectedColl_RF;
                if (isLeft)
                    connectedColl = ref LucidPlayerInfo.connectedColl_LF;

                if (hitInfoThigh.transform != lastCastHit)
                {
                    bool rbvalid = hitInfoThigh.transform.TryGetComponent(out Rigidbody rb);
                    bool collvalid = hitInfoThigh.transform.TryGetComponent(out Collider c);

                    if (rbvalid)
                        connectedRB = rb;
                    else
                        connectedRB = null;
                    if (collvalid)
                        connectedColl = c;
                    else
                        connectedColl = null;
                    lastCastHit = hitInfoThigh.transform;
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
                    LucidPlayerInfo.IK_LF.localRotation = Quaternion.LookRotation(LucidPlayerInfo.pelvis.forward);
                    lastCastHitL = null;
                }
                else
                {
                    LucidPlayerInfo.connectedRB_RF = null;
                    LucidPlayerInfo.IK_RF.localRotation = Quaternion.LookRotation(LucidPlayerInfo.pelvis.forward);
                    lastCastHitR = null;
                }
            }

            if (LucidPlayerInfo.stanceHeight > 0.11f && Vector3.Distance(thighOrigin, Cast) < minCastDist)
            {
                Cast = thighOrigin + (LucidPlayerInfo.pelvis.forward * LucidPlayerInfo.totalLegLength);
                LucidPlayerInfo.connectedRB_LF = null;
                LucidPlayerInfo.connectedRB_RF = null;
            }
            else if (LucidPlayerInfo.stanceHeight < 0.11f)
            {
                Vector3 footNormal = Vector3.up;
                if (thighCast)
                    footNormal = hitInfoThigh.normal;
                else if (shinCast)
                    footNormal = hitInfoShin.normal;

                if (Vector3.Angle(Vector3.up, footNormal) < LucidPlayerInfo.slidePushAngleThreshold)
                    Cast = footpos;
            }

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

            if (normal.y > highSlopeThreshold || LucidInputValueShortcuts.jump || LucidPlayerInfo.climbing || LucidPlayerInfo.stanceHeight < 0.09f)
            {
                if (isLeft)
                    LucidPlayerInfo.footSurfaceL = normal;
                else
                    LucidPlayerInfo.footSurfaceR = normal;

                LucidPlayerInfo.footSurface = normal;
                LucidPlayerInfo.footspace.position = point + (normal * verticalFootAdjust);
                LucidPlayerInfo.footspace.up = normal;
            }

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
            bool armHit = Physics.SphereCast(shoulder.position, legCastThickness / 2, hand.position - shoulder.position, out RaycastHit armHitInfo, Vector3.Distance(hand.position, shoulder.position), LucidShortcuts.geometryMask);

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
    }
}
