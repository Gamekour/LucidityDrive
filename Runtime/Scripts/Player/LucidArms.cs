using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

namespace LucidityDrive
{
    public class LucidArms : MonoBehaviour
    {
        public static LucidArms instance;
        [HideInInspector]
        public bool initialized = false;

        public Transform defaultItemPosesR;
        public Transform defaultItemPosesL;

        [SerializeField]
        float
            shoulderDistance,
            castDistance,
            limitDistance,
            shoulderStiffness,
            firstCastWidth,
            secondCastWidth,
            maxInitialNrmY,
            minDowncastNrmY,
            ungrabBoost,
            climbModeForceThreshold,
            velocityCheatForLucidTools,
            swingStabilization,
            pullSpeed,
            pullDamp,
            maxPullHeight
            ;

        [SerializeField] Transform unRotateL, unRotateR;
        [SerializeField] Transform handTargetL, handTargetR;
        [SerializeField] Transform grabIndicatorL, grabIndicatorR;
        [SerializeField] ConfigurableJoint jointReference;
        [SerializeField] Rigidbody staticGrabRB_L, staticGrabRB_R;
        [SerializeField] LayerMask CastMask;
        [SerializeField] string[] grippyTags;
        private SoftJointLimit sjlewis;
        private EventBox eventBoxL, eventBoxR;
        private ConfigurableJoint anchorL, anchorR;
        private Rigidbody grabbedRB_R, grabbedRB_L;
        private LucidTool lt_R, lt_L;
        private Transform animShoulderL, animShoulderR;
        private Transform targetTransformL, targetTransformR;
        private Transform currentPoseL, currentPoseR;
        private Transform currentPoseParentL, currentPoseParentR;
        private IGrabTrigger lastGrabTriggerL, lastGrabTriggerR;
        private Vector3 grabPositionL, grabPositionR;
        private Vector3 grabForceL, grabForceR;
        private Quaternion grabRotationL, grabRotationR;
        private Quaternion dynamicGrabRotationOffsetL, dynamicGrabRotationOffsetR;
        private Quaternion poseOffsetL, poseOffsetR;
        private bool
            grabL,
            grabR,
            grabWaitL,
            grabWaitR,
            grabLockL,
            grabLockR,
            disableDropL,
            disableDropR,
            grippyL,
            grippyR,
            isPrimaryL,
            isPrimaryR,
            disabling
            = false;
        private float animArmLength = 0;
        private float currentPull = 0;

        private void Awake() => instance = this;

        private void OnEnable()
        {
            if (LucidInputActionRefs.grabL != null)
                ManageInputSubscriptions(true);
            LucidPlayerInfo.OnAssignVismodel.AddListener(OnAssignVismodel);
            LucidPlayerInfo.handTargetL = handTargetL;
            LucidPlayerInfo.handTargetR = handTargetR;
            RespawnInterface.OnRespawn.AddListener(OnRespawn);
        }

        private void OnDisable()
        {
            disabling = true;
            grabWaitL = false;
            grabWaitR = false;
            Ungrab(false);
            Ungrab(true);
            LucidPlayerInfo.climbL = false;
            LucidPlayerInfo.climbR = false;
            LucidPlayerInfo.climbing = false;

            ManageInputSubscriptions(false);
            LucidPlayerInfo.OnAssignVismodel.RemoveListener(OnAssignVismodel);
            RespawnInterface.OnRespawn.RemoveListener(OnRespawn);
        }

        private void ManageInputSubscriptions(bool subscribe)
        {
            if (subscribe)
            {
                LucidInputActionRefs.grabL.started += GrabButtonLeftDown;
                LucidInputActionRefs.grabL.canceled += GrabButtonLeftUp;
                LucidInputActionRefs.grabR.started += GrabButtonRightDown;
                LucidInputActionRefs.grabR.canceled += GrabButtonRightUp;
                LucidInputActionRefs.dropL.started += DropButtonL;
                LucidInputActionRefs.dropR.started += DropButtonR;
            }
            else
            {
                LucidInputActionRefs.grabL.started -= GrabButtonLeftDown;
                LucidInputActionRefs.grabL.canceled -= GrabButtonLeftUp;
                LucidInputActionRefs.grabR.started -= GrabButtonRightDown;
                LucidInputActionRefs.grabR.canceled -= GrabButtonRightUp;
                LucidInputActionRefs.dropL.started -= DropButtonL;
                LucidInputActionRefs.dropR.started -= DropButtonR;
            }
        }

        private void OnRespawn()
        {
            if (lt_L != null)
            {
                lt_L.transform.position = LucidPlayerInfo.pelvis.position;
                grabbedRB_L.velocity = Vector3.zero;
            }
            if (lt_R != null)
            {
                lt_R.transform.position = LucidPlayerInfo.pelvis.position;
                grabbedRB_R.velocity = Vector3.zero;
            }
        }

        private void OnAssignVismodel(LucidVismodel visModel)
        {
            disabling = false;
            animShoulderL = visModel.anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            animShoulderR = visModel.anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            animArmLength = CalculateAnimArmLength(visModel);
            sjlewis.limit = limitDistance * animArmLength;
            currentPoseL = defaultItemPosesL.Find("OneHandedCarry");
            currentPoseR = defaultItemPosesR.Find("OneHandedCarry");

            initialized = true;
        }

        private void Start()
        {
            ManageInputSubscriptions(true);
            sjlewis.limit = limitDistance;
            currentPoseParentL = defaultItemPosesL;
            currentPoseParentR = defaultItemPosesR;
        }

        //in short: uses an initial cast to find the nearest "wall", then another cast to find the top of said wall. If all is good and within reach, then we start asking if we're trying to grab and if so we call those functions up
        private void FixedUpdate()
        {
            if (disabling || animShoulderL == null || animShoulderR == null || !initialized) return;

            if (LucidPlayerInfo.grabValidL)
            {
                unRotateL.position = grabPositionL;
                Vector3 targetdirL = staticGrabRB_L.transform.forward;
                if (staticGrabRB_L.transform.up.y < 0)
                    targetdirL *= -1;
                unRotateL.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(targetdirL, Vector3.up), Vector3.up);
            }
            if (LucidPlayerInfo.grabValidR)
            {
                unRotateR.position = grabPositionR;
                Vector3 targetdirR = staticGrabRB_R.transform.forward;
                if (staticGrabRB_R.transform.up.y < 0)
                    targetdirR *= -1;
                unRotateR.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(targetdirR, Vector3.up), Vector3.up);
            }

            if (grabL && (targetTransformL == null || !targetTransformL.gameObject.activeInHierarchy))
                Ungrab(false);
            if (grabR && (targetTransformR == null || !targetTransformR.gameObject.activeInHierarchy))
                Ungrab(true);

            GrabLogic(false);
            GrabLogic(true);

            Vector3 targetposL = LucidPlayerInfo.pelvis.transform.InverseTransformPoint(animShoulderL.position);
            Vector3 targetposR = LucidPlayerInfo.pelvis.transform.InverseTransformPoint(animShoulderR.position);

            if (anchorL != null)
            {
                anchorL.anchor += (targetposL - anchorL.anchor) * shoulderStiffness;
                grabForceL = anchorL.currentForce;
                if (lt_L != null)
                    grabbedRB_L.AddForce((LucidPlayerInfo.mainBody.velocity - grabbedRB_L.velocity) * velocityCheatForLucidTools);
            }
            else
                grabForceL = Vector3.down * 1000;
            if (anchorR != null)
            {
                anchorR.anchor += (targetposR - anchorR.anchor) * shoulderStiffness;
                grabForceR = anchorR.currentForce;
                if (lt_R != null)
                    grabbedRB_R.AddForce((LucidPlayerInfo.mainBody.velocity - grabbedRB_R.velocity) * velocityCheatForLucidTools);
            }
            else
                grabForceR = Vector3.down * 1000;

            LucidPlayerInfo.grabL = grabL;
            LucidPlayerInfo.grabR = grabR;
            ClimbCheck();

            CalcClimbRelative();

            if (LucidPlayerInfo.climbL && LucidPlayerInfo.climbing)
                ClimbPose(false);
            else
                ItemPose(false);

            if (LucidPlayerInfo.climbR && LucidPlayerInfo.climbing)
                ClimbPose(true);
            else
                ItemPose(true);

            if (grabL || grabR)
                HandPush();

            if (grabIndicatorL != null)
            {
                grabIndicatorL.transform.position = grabPositionL;
                grabIndicatorL.gameObject.SetActive(LucidPlayerInfo.grabValidL && !grabL);
            }
            if (grabIndicatorR != null)
            {
                grabIndicatorR.transform.position = grabPositionR;
                grabIndicatorR.gameObject.SetActive(LucidPlayerInfo.grabValidR && !grabR);
            }
        }

        private void ClimbCheck(bool allowUnclimb = false)
        {
            bool staticGrabL = (grabbedRB_L == null || grabbedRB_L == staticGrabRB_L);
            bool staticGrabR = (grabbedRB_R == null || grabbedRB_R == staticGrabRB_R);

            staticGrabL |= staticGrabRB_L == null;
            staticGrabR |= staticGrabRB_R == null;

            if (!staticGrabL)
            {
                if (grabbedRB_L == null)
                    Ungrab(false);
                else if (!grabbedRB_L.gameObject.activeInHierarchy)
                    Ungrab(false);
                else
                {
                    Vector3 handUp = (grabbedRB_L.rotation * grabRotationL * Vector3.up).normalized;
                    if (handUp.y < minDowncastNrmY && !grippyL)
                        Ungrab(false);
                }

            }
            if (!staticGrabR)
            {
                if (grabbedRB_R == null)
                    Ungrab(true);
                else if (!grabbedRB_R.gameObject.activeInHierarchy)
                    Ungrab(true);
                else
                {
                    Vector3 handUp = (grabbedRB_R.rotation * grabRotationR * Vector3.up).normalized;
                    if (handUp.y < minDowncastNrmY && !grippyR)
                        Ungrab(true);
                }

            }

            float totalGrabForce = 0;
            if (grabL)
                totalGrabForce += grabForceL.y;
            if (grabR)
                totalGrabForce += grabForceR.y;
            bool selfsufficient = totalGrabForce > climbModeForceThreshold;
            selfsufficient |= (staticGrabL || staticGrabR);

            bool supportL = grabForceL.y > climbModeForceThreshold / 2;
            bool supportR = grabForceR.y > climbModeForceThreshold / 2;

            if ((supportL || staticGrabL) && grabL)
                LucidPlayerInfo.climbL = true;
            else if (allowUnclimb)
                LucidPlayerInfo.climbL = false;
            if ((supportR || staticGrabR) && grabR)
                LucidPlayerInfo.climbR = true;
            else if (allowUnclimb)
                LucidPlayerInfo.climbR = false;

            if ((LucidPlayerInfo.climbL || LucidPlayerInfo.climbR) && selfsufficient)
                LucidPlayerInfo.climbing = true;
            else if (allowUnclimb)
                LucidPlayerInfo.climbing = false;
        }

        private void LateUpdate()
        {
            bool dynamicGrabL = (grabbedRB_L != null && grabbedRB_L != staticGrabRB_L);
            bool dynamicGrabR = (grabbedRB_R != null && grabbedRB_R != staticGrabRB_R);

            if (grabR)
            {
                Quaternion effectiveRotationR = grabRotationR;
                if (dynamicGrabR)
                {
                    grabPositionR = grabbedRB_R.transform.TransformPoint(anchorR.connectedAnchor);
                    effectiveRotationR = grabbedRB_R.rotation * dynamicGrabRotationOffsetR;
                }
                LucidPlayerInfo.IK_RH.SetPositionAndRotation(grabPositionR, effectiveRotationR);
            }
            if (grabL)
            {
                Quaternion effectiveRotationL = grabRotationL;
                if (dynamicGrabL)
                {
                    grabPositionL = grabbedRB_L.transform.TransformPoint(anchorL.connectedAnchor);
                    effectiveRotationL = grabbedRB_L.rotation * dynamicGrabRotationOffsetL;
                }
                LucidPlayerInfo.IK_LH.SetPositionAndRotation(grabPositionL, effectiveRotationL);
            }
        }

        private void UpdateItemPose(bool isRight, string pose)
        {
            ref Transform tPoseParent = ref currentPoseParentL;
            if (isRight)
                tPoseParent = ref currentPoseParentR;
            Transform tPose = tPoseParent.Find(pose);
            ref LucidTool lt = ref lt_L;
            if (isRight)
                lt = ref lt_R;
            if (lt != null)
            {
                tPoseParent = isRight ? lt.itemPosesR : lt.itemPosesL;
                bool doPrimary = isRight ? isPrimaryR : isPrimaryL;
                tPose = doPrimary ? lt.ItemPosePrimaryR : lt.ItemPoseSecondaryR;
            }
            else
                tPoseParent = isRight ? defaultItemPosesR : defaultItemPosesL;

            if (tPose == null)
                tPose = tPoseParent.GetChild(0);
            ref Transform currentPose = ref currentPoseL;
            if (isRight)
                currentPose = ref currentPoseR;
            currentPose = tPose;
        }

        private void ItemPose(bool isRight)
        {
            ref Transform itemPoses = ref currentPoseParentL;
            if (isRight)
                itemPoses = ref currentPoseParentR;

            ref Transform handTarget = ref handTargetL;
            if (isRight)
                handTarget = ref handTargetR;

            bool isPrimary = isRight ? isPrimaryR : isPrimaryL;

            Transform currentPose = isRight ? currentPoseR : currentPoseL;

            Transform animShoulder = isRight ? animShoulderR : animShoulderL;

            Quaternion targetRotation = currentPose.rotation;
            Vector3 posePos = currentPose.position;
            if (isPrimary != isRight)
            {
                Vector3 relPos = LucidPlayerInfo.head.InverseTransformPoint(posePos);
                relPos.x *= -1;
                posePos = LucidPlayerInfo.head.TransformPoint(relPos);
                targetRotation *= Quaternion.Euler(transform.forward * 180);
            }
            handTarget.SetPositionAndRotation(posePos, targetRotation);
        }

        private void ClimbPose(bool isRight)
        {
            float pull = (LucidInputValueShortcuts.jump ? 1 : 0);
            float unpull = (LucidInputValueShortcuts.crouch ? 1 : 0);

            Vector2 inputmove = LucidInputValueShortcuts.movement;
            Vector3 moveflat = Vector3.zero;
            moveflat.x = inputmove.x;
            moveflat.z = inputmove.y;
            Vector3 motion = LucidPlayerInfo.pelvis.TransformVector(moveflat);

            Vector3 desiredDir = motion.normalized;
            Vector3 currentVelFlat = LucidPlayerInfo.mainBody.velocity;
            currentVelFlat.y = 0;
            if (motion == Vector3.zero)
                desiredDir = LucidPlayerInfo.pelvis.forward;
            Vector3 perpVelocity = currentVelFlat - Vector3.Project(currentVelFlat, desiredDir);

            motion -= perpVelocity * swingStabilization;

            float pull_add = (pull * pullSpeed * Time.fixedDeltaTime);
            float pull_sub = (unpull * pullSpeed * Time.fixedDeltaTime);

            currentPull = Mathf.Clamp(currentPull + pull_add - pull_sub, 0, maxPullHeight);
            LucidPlayerInfo.swinging = currentPull < 0.05f;

            float handY = isRight ? grabPositionR.y : grabPositionL.y;

            motion.y = ((handY - LucidPlayerInfo.pelvis.position.y) / animArmLength) - maxPullHeight + (currentPull * 2);
            motion *= animArmLength;
            if (!LucidPlayerInfo.swinging)
                motion.y -= LucidPlayerInfo.mainBody.velocity.y * pullDamp;
            else
                motion.y = 0;
            if (LucidInputValueShortcuts.jump)
                motion.y = Mathf.Clamp(motion.y, 0, Mathf.Infinity);
            if (LucidInputValueShortcuts.crouch)
                motion.y = Mathf.Clamp(motion.y, -Mathf.Infinity, 0);

            if (isRight)
                handTargetR.transform.position = grabPositionR - motion;
            else
                handTargetL.transform.position = grabPositionL - motion;
        }

        private void HandPush()
        {
            bool dynamicGrabL = (grabbedRB_L != null && grabbedRB_L != staticGrabRB_L);
            bool dynamicGrabR = (grabbedRB_R != null && grabbedRB_R != staticGrabRB_R);
            if (anchorL != null)
            {
                anchorL.targetPosition = anchorL.transform.InverseTransformPoint(handTargetL.position) - anchorL.anchor;
                Quaternion localhand = Quaternion.Inverse(LucidPlayerInfo.pelvis.rotation) * handTargetL.rotation;
                localhand *= Quaternion.Inverse(dynamicGrabRotationOffsetL);
                if (dynamicGrabL)
                    anchorL.targetRotation = localhand * poseOffsetL;
                else
                    anchorL.targetRotation = Quaternion.identity;
            }
            if (anchorR != null)
            {
                Quaternion localhand = Quaternion.Inverse(LucidPlayerInfo.pelvis.rotation) * handTargetR.rotation;
                localhand *= Quaternion.Inverse(dynamicGrabRotationOffsetR);
                anchorR.targetPosition = anchorR.transform.InverseTransformPoint(handTargetR.position) - anchorR.anchor;
                if (dynamicGrabR)
                    anchorR.targetRotation = localhand * poseOffsetR;
                else
                    anchorR.targetRotation = Quaternion.identity;
            }
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

            ref Vector3 grabPosition = ref grabPositionL;
            if (isRight)
                grabPosition = ref grabPositionR;

            ref Quaternion grabRotation = ref grabRotationL;
            if (isRight)
                grabRotation = ref grabRotationR;

            ref Rigidbody staticRB = ref staticGrabRB_L;
            if (isRight)
                staticRB = ref staticGrabRB_R;

            ref Transform targetTransform = ref targetTransformL;
            if (isRight)
                targetTransform = ref targetTransformR;

            ConfigurableJoint targetanchor = isRight ? anchorR : anchorL;

            if (!grabToCheck)
            {
                bool oldgrab = isRight ? LucidPlayerInfo.grabValidR : LucidPlayerInfo.grabValidL;
                bool validgrab = ClimbScan(isRight, out Vector3 position, out Quaternion rotation, out Transform hitTransform);
                ref IGrabTrigger oldGrabTrigger = ref lastGrabTriggerL;
                if (isRight)
                    oldGrabTrigger = ref lastGrabTriggerR;
                ref IGrabTrigger oldGrabTriggerAlt = ref lastGrabTriggerR;
                if (isRight)
                    oldGrabTriggerAlt = ref lastGrabTriggerL;

                if (validgrab)
                {
                    grabPosition = position;
                    grabRotation = rotation;
                    targetTransform = hitTransform;
                    if (!oldgrab && targetTransform.TryGetComponent(out IGrabTrigger gt))
                    {
                        gt.HoverEvent();
                        oldGrabTrigger = gt;
                    }
                }
                else if (oldgrab && oldGrabTrigger != null)
                {
                    if (oldGrabTrigger != oldGrabTriggerAlt)
                        oldGrabTrigger.UnHoverEvent();

                    oldGrabTrigger = null;
                }

                if (isRight)
                    LucidPlayerInfo.grabValidR = validgrab;
                else
                    LucidPlayerInfo.grabValidL = validgrab;

                bool grabwait = (isRight ? grabWaitR : grabWaitL);
                if (validgrab && grabwait)
                    Grab(isRight);

                staticRB.transform.SetPositionAndRotation(grabPosition, grabRotation);
            }
            else
            {
                if (isRight)
                    LucidPlayerInfo.grabValidR = true;
                else
                    LucidPlayerInfo.grabValidL = true;
            }
        }

        private bool ClimbScan(bool right, out Vector3 position, out Quaternion rotation, out Transform targetTransform)
        {
            Transform cam = LucidPlayerInfo.head;
            Vector3 campos = cam.position;
            Vector3 camfwd = cam.forward;
            Vector3 camright = cam.right;
            float castAdjust = castDistance * animArmLength;

            Vector3 shoulder = campos + (right ? camright * shoulderDistance : -camright * shoulderDistance);

            bool initialHit = Physics.SphereCast(shoulder, firstCastWidth, camfwd, out RaycastHit initialHitInfo, castAdjust, CastMask);

            bool grabbableInitialHit = (initialHit && grippyTags.Contains(initialHitInfo.transform.gameObject.tag));
            initialHit &= ((initialHitInfo.normal.y) < maxInitialNrmY);

            Vector3 projectvector = Vector3.up;
            if (initialHitInfo.normal.y < -0.05f)
                projectvector = -LucidPlayerInfo.pelvis.forward + Vector3.up;
            else if (initialHitInfo.normal.y > 0.05f)
                projectvector = LucidPlayerInfo.pelvis.forward + Vector3.up;

            Vector3 hitvector = Vector3.ProjectOnPlane(projectvector, initialHitInfo.normal).normalized;
            if (Mathf.Abs(initialHitInfo.normal.y) < 0.01f)
                hitvector = Vector3.up;

            float angle = Vector3.Angle(-cam.forward, hitvector);
            float dist = Vector3.Distance(shoulder, initialHitInfo.point);

            //i know this trig stuff looks scary but that's just how it knows where to look when you're dealing with a sloped wall
            float sinC = (dist * Mathf.Sin(Mathf.Deg2Rad * angle)) / castAdjust;
            float A = 180 - (Mathf.Asin(sinC) + angle);
            float newMaxHeight = (Mathf.Sin(Mathf.Deg2Rad * A) * castAdjust) / Mathf.Sin(Mathf.Deg2Rad * angle);

            Vector3 startpoint = initialHitInfo.point + (hitvector.normalized * newMaxHeight);

            bool surfaceCastHit = Physics.SphereCast(startpoint - initialHitInfo.normal * secondCastWidth, secondCastWidth, -hitvector, out RaycastHit surfaceCastHitInfo, newMaxHeight, CastMask);
            bool holeCastHit = false;
            if (surfaceCastHit)
            {
                Vector3 holeCastStart = shoulder;
                holeCastStart.y = surfaceCastHitInfo.point.y + secondCastWidth + (1 - surfaceCastHitInfo.normal.y) + 0.01f;
                holeCastHit = Physics.SphereCast(holeCastStart, secondCastWidth, (surfaceCastHitInfo.point - shoulder).normalized, out RaycastHit holeCastInfo, Vector3.Distance(surfaceCastHitInfo.point, shoulder));
            }
            bool validgrab = initialHit && surfaceCastHit && surfaceCastHitInfo.normal.y > minDowncastNrmY && !holeCastHit;

            if (validgrab)
            {
                position = surfaceCastHitInfo.point;
                Vector3 transformhitinfo = Vector3.ProjectOnPlane(-initialHitInfo.normal, surfaceCastHitInfo.normal);
                rotation = Quaternion.LookRotation(transformhitinfo, surfaceCastHitInfo.normal);
            }
            else
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
            }

            targetTransform = surfaceCastHitInfo.transform;

            if (!validgrab && grabbableInitialHit)
            {
                Vector3 jointfwd = LucidPlayerInfo.pelvis.forward;

                if (Mathf.Abs(initialHitInfo.normal.y) < 0.05f)
                    jointfwd = Vector3.up;
                else if (initialHitInfo.normal.y < 0)
                    jointfwd = -jointfwd;

                targetTransform = initialHitInfo.transform;
                position = initialHitInfo.point;
                Vector3 transformhitinfo = Vector3.ProjectOnPlane(jointfwd, initialHitInfo.normal);
                rotation = Quaternion.LookRotation(transformhitinfo, initialHitInfo.normal);

                validgrab = true;
            }

            if (right)
                grippyR = grabbableInitialHit;
            else
                grippyL = grabbableInitialHit;

            return validgrab;
        }

        private void CalcClimbRelative()
        {
            Vector3 center = Vector3.zero;
            if (LucidPlayerInfo.climbR)
            {
                Vector3 relative = unRotateR.transform.InverseTransformPoint(LucidPlayerInfo.pelvis.position);
                center += relative;
            }
            if (LucidPlayerInfo.climbL)
            {
                Vector3 relative = unRotateL.transform.InverseTransformPoint(LucidPlayerInfo.pelvis.position);
                center += relative;
            }
            if (LucidPlayerInfo.climbL && LucidPlayerInfo.climbR)
            {
                center /= 2;
            }

            Vector3 climbrelative = center;
            climbrelative /= limitDistance;

            LucidPlayerInfo.climbRelative = climbrelative;
        }


        #region Input Events
        private void GrabButtonLeftDown(InputAction.CallbackContext obj)
        {
            if (lt_L != null && grabLockL)
                lt_L.OnUse.Invoke();
            else
            {
                if (LucidPlayerInfo.grabValidL)
                    Grab(false);
                else
                    grabWaitL = true;
            }
        }

        private void GrabButtonRightDown(InputAction.CallbackContext obj)
        {
            if (lt_R != null && grabLockR)
                lt_R.OnUse.Invoke();
            else
            {
                if (LucidPlayerInfo.grabValidR)
                    Grab(true);
                else
                    grabWaitR = true;
            }
        }

        private void GrabButtonLeftUp(InputAction.CallbackContext obj)
        {
            if (lt_L != null && grabLockL)
                lt_L.OnUseUp.Invoke();
            grabWaitL = false;
            if (!grabLockL)
                Ungrab(false);
        }

        private void GrabButtonRightUp(InputAction.CallbackContext obj)
        {
            if (lt_R != null && grabLockR)
                lt_R.OnUseUp.Invoke();
            grabWaitR = false;
            if (!grabLockR)
                Ungrab(true);
        }

        private void DropButtonL(InputAction.CallbackContext obj)
        {
            if (lt_L != null)
                lt_L.OnDrop.Invoke();
            if (grabL && grabLockL && !disableDropL)
            {
                grabWaitL = false;
                Ungrab(false);
            }
        }

        private void DropButtonR(InputAction.CallbackContext obj)
        {
            if (lt_R != null)
                lt_R.OnDrop.Invoke();
            if (grabR && grabLockR && !disableDropR)
            {
                grabWaitR = false;
                Ungrab(true);
            }
        }
        #endregion

        public void ForceGrab(LucidTool lt, bool isRight)
        {
            ref Transform grabTransform = ref targetTransformL;
            if (isRight)
                grabTransform = ref targetTransformR;

            ref Rigidbody staticRB = ref staticGrabRB_L;
            if (isRight)
                staticRB = ref staticGrabRB_R;

            ref Rigidbody grabbedRB = ref grabbedRB_L;
            if (isRight)
                grabbedRB = ref grabbedRB_R;

            grabTransform = lt.transform;

            if (isRight)
                grippyR = true;
            else
                grippyL = true;
            Grab(isRight);
        }

        public void ForceUngrab(bool isRight)
        {
            if (isRight)
                grippyR = false;
            else
                grippyL = false;
            Ungrab(isRight);
        }

        private void Grab(bool isRight)
        {
            bool grablock = isRight ? grabLockR : grabLockL;
            if (!initialized || grablock) return;

            ref Vector3 grabPosition = ref grabPositionL;
            if (isRight)
                grabPosition = ref grabPositionR;

            ref Quaternion grabRotation = ref grabRotationL;
            if (isRight)
                grabRotation = ref grabRotationR;

            ref bool grabLock = ref grabLockL;
            if (isRight)
                grabLock = ref grabLockR;

            ref bool disableDrop = ref disableDropL;
            if (isRight)
                disableDrop = ref disableDropR;

            ref bool isPrimary = ref isPrimaryL;
            if (isRight)
                isPrimary = ref isPrimaryR;

            ref LucidTool lt = ref lt_L;
            if (isRight)
                lt = ref lt_R;

            LucidTool otherLT = isRight ? lt_L : lt_R;

            Transform targetTransform = isRight ? targetTransformR : targetTransformL;

            if (targetTransform.TryGetComponent(out lt))
            {
                isPrimary = (lt != otherLT);
                Transform targetGrip = isPrimary ? lt.PrimaryGripR : lt.SecondaryGripR;
                grabPosition = targetGrip.position;
                grabRotation = targetGrip.rotation;
                if (isRight != isPrimary)
                {
                    Vector3 inverseGrabPosition = lt.transform.InverseTransformPoint(grabPosition);
                    inverseGrabPosition.x *= -1;
                    grabPosition = lt.transform.TransformPoint(inverseGrabPosition);
                    grabRotation *= Quaternion.Euler(transform.forward * 180);
                }
                grabLock = isPrimary ? lt.GrabLockPrimary : lt.GrabLockSecondary;
                disableDrop = lt.disableDrop;
                lt.OnGrab.Invoke();
                lt.held = true;
            }

            CreateConfigurableJoint(isRight, grabPosition, grabRotation, targetTransform);

            ref bool grab = ref grabL;
            if (isRight)
                grab = ref grabR;

            ref bool grabwait = ref grabWaitL;
            if (isRight)
                grabwait = ref grabWaitR;

            IGrabTrigger trig = targetTransform.GetComponent<IGrabTrigger>();
            trig?.GrabEvent();

            if (grabL && grabR)
            {
                UpdateItemPose(false, "TwoHandedCarry");
                UpdateItemPose(true, "TwoHandedCarry");
            }
            else
                UpdateItemPose(isRight, "OneHandedCarry");

            grabwait = false;
            grab = true;
        }

        private void Ungrab(bool isRight)
        {
            ref bool grab = ref grabL;
            if (isRight)
                grab = ref grabR;

            ref bool climb = ref LucidPlayerInfo.climbL;
            if (isRight)
                climb = ref LucidPlayerInfo.climbR;

            ref bool otherClimb = ref LucidPlayerInfo.climbR;
            if (isRight)
                otherClimb = ref LucidPlayerInfo.climbL;

            ref bool grabLock = ref grabLockL;
            if (isRight)
                grabLock = ref grabLockR;

            ref bool disableDrop = ref disableDropL;
            if (isRight)
                disableDrop = ref disableDropR;

            ref Vector3 grabForce = ref grabForceL;
            if (isRight)
                grabForce = ref grabForceR;

            if (!initialized || !grab) return;

            Transform targetTransform = isRight ? targetTransformR : targetTransformL;

            ref Rigidbody grabbedRB = ref grabbedRB_L;
            if (isRight)
                grabbedRB = ref grabbedRB_R;

            ref ConfigurableJoint jointTarget = ref anchorL;
            if (isRight)
                jointTarget = ref anchorR;

            ref LucidTool lt = ref lt_L;
            if (isRight)
                lt = ref lt_R;

            if (lt != null)
                lt.held = false;

            LucidTool otherLT = isRight ? lt_L : lt_R;

            Destroy(jointTarget);

            if (targetTransform != null)
            {
                IGrabTrigger trig = targetTransform.GetComponent<IGrabTrigger>();
                trig?.UngrabEvent();
                grabbedRB = null;
            }

            Vector3 dir = LucidPlayerInfo.head.forward;
            LucidPlayerInfo.mainBody.AddForce(dir * ungrabBoost, ForceMode.Acceleration);

            UpdateItemPose(!isRight, "OneHandedCarry");

            grab = false;
            climb = false;
            lt = null;
            grabLock = false;
            disableDrop = false;

            grabForce = Vector3.zero;
            ClimbCheck(true);

            if (!otherClimb)
                currentPull = 0;
        }

        private void CreateConfigurableJoint(bool isRight, Vector3 grabPosition, Quaternion grabRotation, Transform grabTarget)
        {
            ref Quaternion dynamicGrabRotationOffset = ref dynamicGrabRotationOffsetL;
            if (isRight)
                dynamicGrabRotationOffset = ref dynamicGrabRotationOffsetR;

            ref Quaternion poseOffset = ref poseOffsetL;
            if (isRight)
                poseOffset = ref poseOffsetR;

            ref ConfigurableJoint jointTarget = ref anchorL;
            if (isRight)
                jointTarget = ref anchorR;

            ref Rigidbody grabbedRB = ref grabbedRB_L;
            if (isRight)
                grabbedRB = ref grabbedRB_R;

            ref Rigidbody staticRB = ref staticGrabRB_L;
            if (isRight)
                staticRB = ref staticGrabRB_R;

            ref EventBox eventBox = ref eventBoxL;
            if (isRight)
                eventBox = ref eventBoxR;

            if (jointTarget != null)
                Destroy(jointTarget);

            bool dynamic = false;

            if (grabTarget.TryGetComponent(out Rigidbody grabTargetRB))
            {
                dynamic = true;
                grabbedRB = grabTargetRB;
                dynamicGrabRotationOffset = Quaternion.Inverse(grabbedRB.rotation) * grabRotation;
                poseOffset = Quaternion.Inverse(grabbedRB.rotation) * LucidPlayerInfo.pelvis.rotation;

                eventBox = grabTarget.gameObject.GetComponent<EventBox>();
                if (eventBox == null)
                    eventBox = grabTarget.gameObject.AddComponent<EventBox>();
                eventBox.onCollisionExit = new UnityEvent<Collision>();
                eventBox.onHover = new UnityEvent();
                eventBox.onUnHover = new UnityEvent();
                if (isRight)
                    eventBox.onCollisionExit.AddListener(CollisionExitCallbackR);
                else
                    eventBox.onCollisionExit.AddListener(CollisionExitCallbackL);

                LayerMask ignoreMask = new();
                ignoreMask |= (1 << 3);

                eventBox.ignoreLayers = ignoreMask;
            }
            else
            {
                grabbedRB = staticRB;
                poseOffset = Quaternion.identity;
                dynamicGrabRotationOffset = Quaternion.identity;
            }

            jointTarget = LucidShortcuts.AddComponent(LucidPlayerInfo.pelvis.gameObject, jointReference);
            jointTarget.autoConfigureConnectedAnchor = false;
            jointTarget.linearLimit = sjlewis;
            jointTarget.connectedBody = grabbedRB;
            jointTarget.enableCollision = true;
            if (dynamic)
                jointTarget.connectedAnchor = grabTarget.InverseTransformPoint(grabPosition);
            else
            {
                JointDrive disableDrive = new JointDrive();
                disableDrive.positionSpring = 0;
                disableDrive.positionDamper = 0;
                jointTarget.angularXDrive = disableDrive;
                jointTarget.angularYZDrive = disableDrive;
            }
        }

        public void CollisionExitCallbackL(Collision c)
        {
            ClimbCheck(true);
        }

        public void CollisionExitCallbackR(Collision c)
        {
            ClimbCheck(true);
        }
    }
}