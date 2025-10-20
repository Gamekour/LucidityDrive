using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

class GFG : IComparer<RaycastHit>
{
    public int Compare(RaycastHit x, RaycastHit y)
    {
        if (x.point.y == 0 || y.point.y == 0)
            return 0;
        else
            return x.point.y.CompareTo(y.point.y);
    }
}

namespace LucidityDrive
{
    public class Legs : MonoBehaviour
    {
        private MovementSettings m_movementSettings;
        public MovementSettings MovementSettings
        {
            get { return m_movementSettings; }
            set { m_movementSettings = value; CopyValues(); } //copy values any time the movement settings are changed
        }

        [Header("References")]

        [Tooltip("Movement settings to load on Start")]
        [SerializeField] MovementSettings defaultMovementSettings;

        [Tooltip("Enable this to sprint by default, and walk when the sprint key is held")]
        public bool autoSprint = true;
        [Tooltip("Enable this to disable the default sprint implementation (in case you want to run your own logic, i.e. with the stamina system)")]
        public bool overrideSprint = false;
        [Tooltip("Enable this to change the jump behavior such that holding the jump button causes you to crouch, and letting go actually jumps. Easier for new players, but inhibits advanced manuevers.")]
        public bool jumpPrepare = false;

        [HideInInspector]
        public bool sprintOverride = false;

        [Tooltip("Left Leg Space transform used for certain calculations")]
        [SerializeField] Transform legSpaceL;
        [Tooltip("Left Leg Space transform used for certain calculations")]
        [SerializeField] Transform legSpaceR;
        [Tooltip("Hip Space transform used for certain calculations")]
        [SerializeField] Transform hipSpace;
        [Tooltip("Foot Space transform used for certain calculations")]
        [SerializeField] Transform footSpace;
        [Tooltip("Virtual Floor transform used for certain calculations")]
        [SerializeField] Transform vFloor;

        [Tooltip("Hip collider")]
        [SerializeField] CapsuleCollider pelvisCollider;

        [Tooltip("Input binding to update this script with current movement setting values")]
        [SerializeField] InputAction reloadMovementSettings;

        //parameters copied from movementsettings
        private float
            pelvisRotationSpeed,
            probeDepth,
            maxForceScale,
            forceSmoothness,
            maxProbeOffset,
            jumpForceScale,
            probeScale,
            moveSpeed,
            friction,
            footSlideStrength,
            slopeTilt,
            probeXMinimumOffset,
            probeZMinimumOffset,
            targetHeightByPositiveSlope,
            targetHeightByNegativeSlope,
            targetHeightByNegativeSlopeClamp,
            moveBurst,
            moveSpeedCrawling,
            moveSpeedCrouched,
            flightSpeed,
            flightDrag,
            sprintScale,
            directionalJumpStrength,
            directionalJumpBoost,
            jumpTilt,
            slidePushStrength,
            climbTilt,
            strafeWalkStartThreshold,
            strafeWalkSpeedMult,
            maxAirAcceleration,
            jumpGravity,
            fallGravity,
            aerialMovementSpeed,
            aerialDrag,
            slidePushAngleThreshold,
            airTurnAssist,
            surfaceMagnetismBySlope,
            highSlopeThreshold,
            targetHeightScale,
            minLegAdjust,
            climbLegAdjust,
            slideBoostVertical,
            slideBoostHorizontal,
            maxFreeLookAngle
            = 0;

        private Rigidbody rb;
        private Vector3 bodyCollisionNrm;
        private float animPhase = 0;
        private bool stuckBackSlide = false;
        private bool preparingJump = false;
        private bool releaseJumping = false;

        public const float stanceHeightCrouched = 0.5f;
        public const float stanceHeightCrawl = 0.1f;

        private void Awake()
        {
            MovementSettings = defaultMovementSettings;
            rb = GetComponent<Rigidbody>();
            SetPlayerInfoReferences();
        }

        private void OnEnable()
        {
            reloadMovementSettings.Enable();
            reloadMovementSettings.started += (InputAction.CallbackContext obj) => CopyValues();
            PlayerInfo.OnInputsReady.AddListener(OnInputsReady);
        }

        private void OnInputsReady()
        {
            LucidInputActionRefs.jump.canceled += OnJumpCancelled;
        }

        private void OnDisable()
        {
            reloadMovementSettings.Disable();
            reloadMovementSettings.started -= null;
            LucidInputActionRefs.jump.canceled -= OnJumpCancelled;
            PlayerInfo.OnInputsReady.RemoveListener(OnInputsReady);
            PlayerInfo.flying = false; //this is a fix for a bug occurring when you switch scenes while in a flight zone - i may eventually have a function to reset all temporary values in playerinfo on scene change
            PlayerInfo.pelvisCollision = false;
        }

        private void FixedUpdate()
        {
            if (rb.isKinematic || PlayerInfo.vismodelRef == null || !PlayerInfo.animModelInitialized) return; //this could technically be optimized by delegating it to a bool that only updates when isKinematic is updated, but that's too much work and i don't think this is much of a perf hit

            PseudoWalk();

            Vector3 headflat = PlayerInfo.head.forward;
            headflat.y = 0;
            if (!LucidInputValueShortcuts.freeLook || Vector3.Angle(headflat.normalized, transform.forward) > maxFreeLookAngle)
                RotationLogic();

            Vector3 velflathip = hipSpace.InverseTransformVector(rb.velocity);
            velflathip.y = 0;

            //all of these need to eventually be delegated to bools
            bool inputCrawl = (LucidInputValueShortcuts.bslide && !stuckBackSlide);
            bool inputBackslide = LucidInputValueShortcuts.slide || stuckBackSlide;

            if (inputCrawl && inputBackslide)
                inputBackslide = false;

            if (inputBackslide && !PlayerInfo.pelvisCollision)
                stuckBackSlide = true;

            PlayerInfo.slidingBack = inputBackslide;
            PlayerInfo.slidingForward = inputCrawl;

            bool inputCrouch = LucidInputValueShortcuts.crouch;
            bool inputJump = LucidInputValueShortcuts.jump;
            if (inputJump && !releaseJumping && !preparingJump && PlayerInfo.grounded && !PlayerInfo.climbing)
                preparingJump = true;
            PlayerInfo.isJumping = PlayerInfo.grounded && ((inputJump && !jumpPrepare) || releaseJumping);
            bool inputSprint = LucidInputValueShortcuts.sprint;
            if (overrideSprint)
                inputSprint = sprintOverride;
            else if (autoSprint)
                inputSprint = !inputSprint;
            bool doGroundLogic = PlayerInfo.grounded && PlayerInfo.footSurface.y >= -0.001f;

            float newStanceHeight = 1;

            if (inputBackslide || PlayerInfo.head.up.y < PlayerInfo.camUpsideDownThreshold || PlayerInfo.swinging)
                newStanceHeight = 0;
            else if (inputCrawl)
                newStanceHeight = stanceHeightCrawl;
            else if (inputCrouch || (inputJump && jumpPrepare))
                newStanceHeight = stanceHeightCrouched;

            newStanceHeight = Mathf.Clamp(newStanceHeight, 0, PlayerInfo.maxStanceHeight);

            Vector3 destination = PlayerInfo.autoPilotDestination;
            if (PlayerInfo.autoPilotTransformDestination != null)
                destination = PlayerInfo.autoPilotTransformDestination.position;
            if (PlayerInfo.autoPilot && Vector3.Distance(transform.position, destination) < (PlayerInfo.totalLegLength * 3))
                newStanceHeight = Mathf.Clamp01(((destination.y - transform.position.y) / PlayerInfo.totalLegLength) + 0.5f);

            PlayerInfo.stanceHeight = newStanceHeight;

            bool doingLegPush = false;
            if (PlayerInfo.flying)
                FlightCalc();
            else if (!PlayerInfo.grounded)
            {
                if (!inputJump) rb.AddForce(Vector3.up * (fallGravity - Physics.gravity.y), ForceMode.Acceleration);
                else
                    rb.AddForce(Vector3.up * (jumpGravity - Physics.gravity.y), ForceMode.Acceleration);

                AirCalc();
            }
            else if (doGroundLogic)
            {
                if (PlayerInfo.stanceHeight < 0.09f)
                    SlidePush();
                else
                {
                    LegPush(inputCrouch, inputJump, inputSprint);
                    doingLegPush = true;
                }
            }
            if (inputBackslide && !PlayerInfo.pelvisCollision)
            {
                Vector3 horizontalForce = PlayerInfo.pelvis.forward * slideBoostHorizontal;
                if (!doGroundLogic)
                    horizontalForce = Vector3.zero;
                Vector3 verticalForce = Vector3.up * slideBoostVertical;
                rb.AddForce(verticalForce + horizontalForce, ForceMode.Acceleration);
            }
            if (!doingLegPush || inputCrouch)
            {
                PlayerInfo.probePattern = 0;
                releaseJumping = false;
                preparingJump = false;
            }
        }

        private void OnJumpCancelled(InputAction.CallbackContext obj)
        {
            if (!releaseJumping && preparingJump)
            {
                releaseJumping = true;
                preparingJump = false;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == 6)
            {
                PlayerInfo.flying = false;
                rb.useGravity = true;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.layer == 6)
            {
                PlayerInfo.flying = true;
                rb.useGravity = false;
            }
        }

        //self explanatory, updates playerinfo with what this script knows
        private void SetPlayerInfoReferences()
        {
            PlayerInfo.legRef = this;
            PlayerInfo.mainBody = rb;
            PlayerInfo.pelvis = transform;
            PlayerInfo.pelvisColl = pelvisCollider;
            PlayerInfo.hipspace = hipSpace;
            PlayerInfo.footspace = footSpace;
            PlayerInfo.legspaceL = legSpaceL;
            PlayerInfo.legspaceR = legSpaceR;
        }

        //copies values from movement settings to this script, using property names to match values
        public void CopyValues()
        {
            Type TMoveSettings = typeof(MovementSettings);
            Type TLegs = typeof(Legs);
            FieldInfo[] fields = TMoveSettings.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var field in fields)
            {
                // Find the corresponding field in the destination object
                FieldInfo destField = TLegs.GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                if (destField != null && destField.FieldType == field.FieldType)
                {
                    // Copy the value from the source field to the destination field
                    object value = field.GetValue(MovementSettings);
                    destField.SetValue(this, value);
                }
            }
            PlayerInfo.moveSpeed = moveSpeed;
            PlayerInfo.slidePushAngleThreshold = slidePushAngleThreshold;
        }

        public void ActivateAutopilot(Vector3 destination)
        {
            PlayerInfo.autoPilotTransformDestination = null;
            PlayerInfo.autoPilotDestination = destination;
            PlayerInfo.autoPilot = true;
        }

        public void ActivateAutopilot(Transform destination)
        {
            PlayerInfo.autoPilotTransformDestination = destination;
            PlayerInfo.autoPilotDestination = Vector3.zero;
            PlayerInfo.autoPilot = true;
        }

        public void DeactivateAutopilot()
        {
            PlayerInfo.autoPilot = false;
        }

        //calculates rotation forces necessary for aligning hips with head at an appropriate speed
        private void RotationLogic()
        {
            //gets forward head vector
            Vector3 vecHeadFlat = PlayerInfo.head.forward;
            if (PlayerInfo.head.up.y < 0)
                vecHeadFlat = -vecHeadFlat;
            vecHeadFlat.y = 0;

            float angle = Vector3.SignedAngle(transform.forward, vecHeadFlat, Vector3.up);
            if (Mathf.Abs(angle) < 0.05f)
            {
                transform.Rotate(Vector3.up * angle);
                rb.angularVelocity = Vector3.zero;
            }
            else
                rb.AddTorque(angle * pelvisRotationSpeed * Time.fixedDeltaTime * Vector3.up);
        }

        //handles movement while midair, not to be confused with flight logic
        private void AirCalc()
        {
            Vector2 moveVector = LucidInputValueShortcuts.movement;
            Vector3 moveFlat = Vector3.zero;
            moveFlat.x = moveVector.x;
            moveFlat.z = moveVector.y;
            Vector3 flattened = PlayerInfo.head.TransformVector(moveFlat);
            flattened.y = 0;
            flattened.Normalize();

            if (LucidInputValueShortcuts.slide || LucidInputValueShortcuts.bslide)
                flattened = Vector3.zero;

            // Apply air drag
            if (aerialDrag != 0)
            {
                float aerialDragMultiplier = 1f / (1f + (aerialDrag * Time.fixedDeltaTime));
                float vely = rb.velocity.y;
                rb.velocity *= aerialDragMultiplier;
                Vector3 vel = rb.velocity;
                vel.y = vely;
                rb.velocity = vel;
            }

            // Calculate current horizontal velocity
            Vector3 velflat = rb.velocity;
            velflat.y = 0;

            // Project current velocity onto desired movement direction
            Vector3 projectedVelocity = Vector3.Project(velflat, flattened);

            // Calculate the difference between projected velocity and desired movement
            Vector3 velocityDifference = (flattened * aerialMovementSpeed) - projectedVelocity;

            // Cap the magnitude of the velocity difference
            if (velflat.magnitude > maxAirAcceleration)
            {
                float angle = Vector3.Angle(velflat, flattened) / 180;
                velocityDifference = aerialMovementSpeed * angle * velocityDifference.normalized;
                velocityDifference += (PlayerInfo.pelvis.forward * angle * velflat.magnitude * airTurnAssist);
            }

            // Apply the force
            rb.AddForce(velocityDifference, ForceMode.Acceleration);

            PlayerInfo.connectedRB_LF = null;
            PlayerInfo.connectedRB_RF = null;
        }

        //handles movement while flying
        private void FlightCalc()
        {
            Vector2 moveVector = LucidInputValueShortcuts.movement;
            Vector3 moveFlat = Vector3.zero;
            moveFlat.x = moveVector.x;
            moveFlat.z = moveVector.y;
            moveFlat = transform.TransformVector(moveFlat);
            bool jump = LucidInputValueShortcuts.jump;
            bool crouch = LucidInputValueShortcuts.crouch;

            float yclampmin;
            float yclampmax;

            if (jump)
            {
                yclampmax = Mathf.Infinity;
                yclampmin = 1;
            }
            else if (crouch)
            {
                yclampmax = -1;
                yclampmin = -Mathf.Infinity;
            }
            else
            {
                yclampmax = Mathf.Infinity;
                yclampmin = -Mathf.Infinity;
            }
            moveFlat.y = Mathf.Clamp(moveFlat.y, yclampmin, yclampmax);
            float flightDragMultiplier = 1f / (1f + (flightDrag * Time.fixedDeltaTime));
            rb.velocity *= flightDragMultiplier;
            rb.AddForce(moveFlat * flightSpeed, ForceMode.Acceleration);
        }

        //handles the forces necessary to keep the player upright and moving
        private void LegPush(bool inputCrouch, bool inputJump, bool inputSprint)
        {
            Vector3 velflat = rb.velocity;
            velflat.y = 0;

            Vector2 moveVector = LucidInputValueShortcuts.movement;
            Vector3 moveFlat = Vector3.zero;
            moveFlat.x = moveVector.x;
            moveFlat.z = moveVector.y;
            if (PlayerInfo.autoPilot)
            {
                if (moveVector.magnitude > 0.1f)
                    PlayerInfo.autoPilot = false;
                else
                {
                    Vector3 destination = PlayerInfo.autoPilotDestination;
                    if (PlayerInfo.autoPilotTransformDestination != null)
                        destination = PlayerInfo.autoPilotTransformDestination.position;
                    moveFlat = transform.InverseTransformDirection(destination - transform.position);
                    moveFlat.y = 0;
                    moveFlat = Vector3.ClampMagnitude(moveFlat, 1);
                }
            }

            float surfaceMagnetism = (1 - Mathf.Abs(hipSpace.up.y)) * surfaceMagnetismBySlope;
            rb.AddForce(-hipSpace.up * surfaceMagnetism, ForceMode.Acceleration);

            bool isRight = animPhase > 0.5f;

            Transform tStep = isRight ? PlayerInfo.IK_RF : PlayerInfo.IK_LF;
            Rigidbody tStepped = isRight ? PlayerInfo.connectedRB_RF : PlayerInfo.connectedRB_LF;
            Vector3 pointVelocity = Vector3.zero;
            if (tStep != null && tStepped != null) pointVelocity = tStepped.GetPointVelocity(tStep.position);

            float t = hipSpace.TransformVector(moveFlat).normalized.y;

            if (PlayerInfo.climbing)
                t = climbTilt;
            else if (inputJump && !PlayerInfo.disableJump)
                t = jumpTilt;
            else
                t *= slopeTilt;

            if ((footSpace.up.y < highSlopeThreshold && !inputJump && !PlayerInfo.climbing) || PlayerInfo.probePattern == 1)
                t = 0;

            Vector3 pushdir = Vector3.Lerp(Vector3.up, footSpace.up, t);

            Debug.DrawRay(footSpace.position, pushdir, Color.red);

            float downness = Mathf.Clamp01(1 - footSpace.up.y) * Mathf.Abs(footSpace.up.x);
            float movedownamount = Mathf.Clamp((hipSpace.TransformVector(moveFlat).normalized.y * rb.velocity.magnitude), -targetHeightByNegativeSlopeClamp, 0);
            movedownamount *= targetHeightByNegativeSlope;
            movedownamount -= downness;

            float legLength = PlayerInfo.totalLegLength;

            float moveupamount = Mathf.Clamp01(footSpace.TransformVector(moveFlat).y);
            moveupamount *= targetHeightByPositiveSlope;

            float hipheight = PlayerInfo.animationModel.GetBoneTransform(HumanBodyBones.Hips).position.y;
            float floorheight = PlayerInfo.animationModel.rootPosition.y;
            float legadjust = hipheight - floorheight;

            if (PlayerInfo.isJumping && !PlayerInfo.disableJump && !PlayerInfo.climbing)
            {
                legadjust = Mathf.Infinity;
                rb.AddForce(Vector3.up * (jumpGravity - Physics.gravity.y));
            }
            else if (PlayerInfo.climbing)
            {
                legadjust = climbLegAdjust;
            }
            else
            {
                legadjust *= 1 + movedownamount + moveupamount;
                legadjust = Mathf.Clamp(legadjust, 0, legLength);
            }

            legadjust *= targetHeightScale;

            legadjust = Mathf.Clamp(legadjust, minLegAdjust, Mathf.Infinity);

            float relativeheight = Vector3.Project(hipSpace.position - footSpace.position, pushdir).magnitude;
            if (Vector3.Distance(hipSpace.position, footSpace.position) < 0.1f)
                relativeheight = 0;
            PlayerInfo.relativeHeight = relativeheight;
            float heightratio = relativeheight / legadjust;
            heightratio = Mathf.Clamp(heightratio, 0, Mathf.Infinity);

            float forceadjust = maxForceScale;
            if (LucidInputValueShortcuts.jump && !PlayerInfo.disableJump)
                forceadjust *= jumpForceScale;

            float currentY = rb.velocity.y;
            float targetY = Mathf.LerpUnclamped(forceadjust, 0, heightratio);
            float diff = targetY - currentY;

            float gravfactor = Vector3.Project(-Physics.gravity, pushdir).y;

            Vector3 calc = pushdir * (gravfactor + (diff * forceSmoothness) + pointVelocity.y);
            NrmSlide(calc * rb.mass, out Vector3 slide, out float nrm);
            slide = -slide;
            Vector3 dir = hipSpace.TransformVector(moveFlat);
            if (dir.magnitude == 0)
                dir = -rb.velocity;
            Vector3 current = rb.velocity - pointVelocity;
            current *= rb.mass;
            NrmSlide(current, out Vector3 velslide, out float velnrm);
            slide += (Vector3.Angle(rb.velocity, dir) / 180) * footSlideStrength * velslide;
            nrm += velnrm;

            PlayerInfo.alignment = 1 - (Mathf.Clamp01(Vector3.Angle(velflat, dir) / 90) * Mathf.Clamp01(velflat.magnitude / (moveSpeed * sprintScale)));
            if (velflat.magnitude < 0.1f)
                PlayerInfo.alignment = 1;
            PlayerInfo.isSprinting = inputSprint && !PlayerInfo.disableSprint;

            float moveadjust = moveSpeed;
            if (PlayerInfo.stanceHeight >= stanceHeightCrouched - 0.01f)
            {
                float tStance = PlayerInfo.stanceHeight;
                tStance -= stanceHeightCrouched;
                tStance *= 2;
                moveadjust = Mathf.Lerp(moveSpeedCrouched, moveSpeed, tStance);
            }
            else
                moveadjust = moveSpeedCrawling;
            if (PlayerInfo.isSprinting)
                moveadjust *= sprintScale;

            float t_strafe = Mathf.Clamp01(Mathf.Abs(moveFlat.normalized.x / (1 - strafeWalkStartThreshold)) - strafeWalkStartThreshold);
            moveadjust *= Mathf.Lerp(1, strafeWalkSpeedMult, t_strafe);

            float diffmag = 1 - Mathf.Clamp01(rb.velocity.magnitude / moveadjust);
            moveadjust *= 1 + (diffmag * moveBurst);

            float jumpadjust = (inputJump && !PlayerInfo.disableJump) ? directionalJumpStrength : 1;
            Vector3 movetarget = jumpadjust * moveadjust * hipSpace.TransformVector(moveFlat);
            Vector3 dirJumpBoost = directionalJumpBoost * hipSpace.TransformVector(moveFlat) * rb.mass;
            dirJumpBoost *= (inputJump && !PlayerInfo.disableJump) ? 1 : 0;
            Vector3 relativevel = hipSpace.InverseTransformVector(rb.velocity);
            relativevel.y = 0;
            relativevel = hipSpace.TransformVector(relativevel);
            Vector3 movediff = movetarget - relativevel;
            Vector3 surfaction = -movediff;
            slide += surfaction * rb.mass;

            nrm = Mathf.Clamp(nrm, 0, Mathf.Infinity);

            float frictioncalc = friction;

            float fmax = Mathf.Sqrt(nrm) * rb.mass * friction;
            PlayerInfo.traction = Mathf.Clamp01(fmax / slide.magnitude);
            Vector3 pushcalc = nrm * PlayerInfo.footSurface;
            Vector3 slidecalc = Vector3.ClampMagnitude(-slide, fmax);
            PlayerInfo.currentFootSlide = slidecalc;
            PlayerInfo.currentLegPush = pushcalc;

            Vector3 objectforce = -(slidecalc + pushcalc);

            if (PlayerInfo.connectedRB_LF == null && PlayerInfo.connectedRB_RF == null)
            {
                rb.AddForce(slidecalc + pushcalc + dirJumpBoost, ForceMode.Force);
            }
            else
            {
                Transform footIK = isRight ? PlayerInfo.IK_RF : PlayerInfo.IK_LF;
                Rigidbody connectedRB = isRight ? PlayerInfo.connectedRB_RF : PlayerInfo.connectedRB_LF;
                if (footIK != null && connectedRB != null)
                {
                    connectedRB.AddForceAtPosition(objectforce, footIK.position, ForceMode.Force);

                    Vector3 resistance = objectforce - (connectedRB.GetPointVelocity(footIK.position) / connectedRB.mass);
                    resistance *= -1;
                    resistance = Vector3.ClampMagnitude(resistance, objectforce.magnitude);
                    PlayerInfo.mainBody.AddForce(resistance, ForceMode.Force);
                }
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            Arms.instance.ClimbCheck(true);
            bodyCollisionNrm = collision.contacts[0].normal;
            bool tooSteep = !LucidInputValueShortcuts.jump && bodyCollisionNrm.y < highSlopeThreshold;
            if (bodyCollisionNrm.y >= highSlopeThreshold && !tooSteep)
            {
                PlayerInfo.pelvisCollision = true;

                if (!PlayerInfo.grounded)
                {
                    PlayerInfo.hipspace.up = bodyCollisionNrm;
                    PlayerInfo.footspace.up = bodyCollisionNrm;
                }

                stuckBackSlide = false;
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            PlayerInfo.pelvisCollision = false;
        }

        private void SlidePush()
        {
            PlayerInfo.surfaceAngle = Vector3.Angle(Vector3.up, PlayerInfo.footspace.up);

            if (PlayerInfo.surfaceAngle < slidePushAngleThreshold)
                return;

            Vector3 diffL = legSpaceL.position - PlayerInfo.IK_LF.position;
            Vector3 diffR = legSpaceR.position - PlayerInfo.IK_RF.position;
            Vector3 avg = (diffL + diffR) / 2;
            float strength = 1 - Mathf.Clamp01(avg.magnitude / PlayerInfo.totalLegLength);
            avg = avg.normalized * strength;

            rb.AddForce(avg * slidePushStrength, ForceMode.Acceleration);
        }

        //quickly splits a vector into its movement along and against the surface of footspace
        private void NrmSlide(Vector3 target, out Vector3 slide, out float nrm)
        {
            Vector3 relative = footSpace.InverseTransformVector(target);
            slide = relative;
            slide.y = 0;
            slide = footSpace.TransformVector(slide);
            nrm = relative.y;
        }

        //mostly handles the calculation of the virtual floor and leg animations
        private void PseudoWalk()
        {

            Vector2 moveVector = LucidInputValueShortcuts.movement;
            Vector3 moveFlat = Vector3.zero;
            moveFlat.x = moveVector.x;
            moveFlat.z = moveVector.y;


            Vector3 willflat = (PlayerInfo.pelvis.TransformVector(moveFlat) * (moveSpeed / 2));
            willflat.y = 0;

            Vector3 velflat = rb.velocity;
            velflat.y = 0;

            Vector3 legR = legSpaceR.position;
            legR += legSpaceR.up * 0.1f;
            Vector3 legL = legSpaceL.position;
            legL += legSpaceL.up * 0.1f;

            float downadjust = transform.position.y - probeDepth;

            float highSlopeProbeMult = LucidInputValueShortcuts.jump ? 2 : 1;

            Vector3 probeN = transform.position + (transform.forward * Mathf.Clamp(Mathf.Abs(moveFlat.z) * probeScale, probeZMinimumOffset * highSlopeProbeMult, Mathf.Infinity));
            probeN.y = (Vector3.up * downadjust).y;
            Vector3 probeS = transform.position - (transform.forward * Mathf.Clamp(Mathf.Abs(moveFlat.z) * probeScale, probeZMinimumOffset * highSlopeProbeMult, Mathf.Infinity));
            probeS.y = (Vector3.up * downadjust).y;
            Vector3 probeE = transform.position + (transform.right * Mathf.Clamp(Mathf.Abs(moveFlat.x) * probeScale, probeXMinimumOffset * highSlopeProbeMult, Mathf.Infinity));
            probeE.y = (Vector3.up * downadjust).y;
            Vector3 probeW = transform.position - (transform.right * Mathf.Clamp(Mathf.Abs(moveFlat.x) * probeScale, probeXMinimumOffset * highSlopeProbeMult, Mathf.Infinity));
            probeW.y = (Vector3.up * downadjust).y;

            //probe Z
            Ray N = new(transform.position, probeN - transform.position);
            Ray S = new(transform.position, probeS - transform.position);
            CastWalk(N, S, out RaycastHit hitN, out RaycastHit hitS, -1);

            //probe X
            Ray E = new(legR, probeE - transform.position);
            Ray W = new(legL, probeW - transform.position);
            CastWalk(E, W, out RaycastHit hitE, out RaycastHit hitW, -1);

            float diffZ = Mathf.Abs(hitN.point.y - hitS.point.y);
            float diffX = Mathf.Abs(hitW.point.y - hitE.point.y);

            float minNrmY = LucidInputValueShortcuts.jump ? 0 : highSlopeThreshold;

            List<RaycastHit> results = new();
            if (diffZ < diffX)
            {
                if (hitN.point.sqrMagnitude > float.Epsilon && hitN.distance < maxProbeOffset && hitN.normal.y >= minNrmY)
                    results.Add(hitN);
                if (hitS.point.sqrMagnitude > float.Epsilon && hitS.distance < maxProbeOffset && hitS.normal.y >= minNrmY)
                    results.Add(hitS);
                if (hitE.point.sqrMagnitude > float.Epsilon && hitE.distance < maxProbeOffset && hitE.normal.y >= minNrmY)
                    results.Add(hitE);
                if (hitW.point.sqrMagnitude > float.Epsilon && hitW.distance < maxProbeOffset && hitW.normal.y >= minNrmY)
                    results.Add(hitW);
            }
            else
            {
                if (hitE.point.sqrMagnitude > float.Epsilon && hitE.distance < maxProbeOffset && hitE.normal.y >= minNrmY)
                    results.Add(hitE);
                if (hitW.point.sqrMagnitude > float.Epsilon && hitW.distance < maxProbeOffset && hitW.normal.y >= minNrmY)
                    results.Add(hitW);
                if (hitN.point.sqrMagnitude > float.Epsilon && hitN.distance < maxProbeOffset && hitN.normal.y >= minNrmY)
                    results.Add(hitN);
                if (hitS.point.sqrMagnitude > float.Epsilon && hitS.distance < maxProbeOffset && hitS.normal.y >= minNrmY)
                    results.Add(hitS);
            }

            byte probePattern = 0;
            bool containsN = results.Contains(hitN);
            bool containsS = results.Contains(hitS);
            bool containsE = results.Contains(hitE);
            bool containsW = results.Contains(hitW);
            if (containsN && containsS && !containsE && !containsW)
                probePattern = 1;
            else if (!containsN && !containsS && containsE && containsW)
                probePattern = 2;
            else if (results.Count == 0)
                probePattern = 3;
            else
                probePattern = 0;
            PlayerInfo.probePattern = probePattern;

            Vector3 normal = Vector3.zero;
            Vector3 center = Vector3.zero;
            switch (results.Count)
            {
                case 4:
                    Vector3 a = results[0].point;
                    Vector3 b = results[1].point;
                    Vector3 c;

                    Vector3 pos1 = (a + b + results[2].point) / 3;
                    Vector3 pos2 = (a + b + results[3].point) / 3;
                    Vector3 pos3 = transform.position;

                    if (Vector3.Distance(pos3, pos2) >= Vector3.Distance(pos3, pos1))
                        c = results[2].point;
                    else
                        c = results[3].point;

                    normal = Vector3.Cross(b - a, c - a);
                    center = (a + b + c) / 3;
                    normal.Normalize();
                    if (normal.y < 0)
                        normal = -normal;

                    Debug.DrawLine(a, b, Color.blue);
                    Debug.DrawLine(b, c, Color.blue);
                    Debug.DrawLine(c, a, Color.blue);

                    break;

                case 3:
                    Vector3 a1 = results[0].point;
                    Vector3 b1 = results[1].point;
                    Vector3 c1 = results[2].point;

                    float angle1 = Vector3.Angle((b1 - a1).normalized, (c1 - a1).normalized);
                    if (angle1 < 1f)
                    {
                        c1 = (a1 + b1) / 2;
                        c1 += transform.forward * 0.01f;
                    }

                    normal = Vector3.Cross(b1 - a1, c1 - a1);
                    center = (a1 + b1 + c1) / 3;
                    normal.Normalize();
                    if (normal.y < 0)
                        normal = -normal;

                    Debug.DrawLine(a1, b1, Color.cyan);
                    Debug.DrawLine(b1, c1, Color.cyan);
                    Debug.DrawLine(c1, a1, Color.cyan);

                    break;

                case 2:
                    Vector3 dir = hipSpace.TransformVector(moveFlat);
                    if (dir.sqrMagnitude > float.Epsilon)
                    {
                        Vector3 diff1 = results[0].point - transform.position;
                        Vector3 diff2 = results[1].point - transform.position;
                        float diffangle1 = Vector3.Angle(dir, diff1);
                        float diffangle2 = Vector3.Angle(dir, diff2);
                        if (probePattern == 1)
                        {
                            normal = Vector3.up;
                            center = results[0].point;
                        }
                        else if (diffangle1 < diffangle2)
                        {
                            normal = results[0].normal;
                            center = results[0].point;
                        }
                        else
                        {
                            normal = results[1].normal;
                            center = results[1].point;
                        }
                    }
                    else
                    {
                        float dist1 = Vector3.Distance(transform.position, results[0].point);
                        float dist2 = Vector3.Distance(transform.position, results[1].point);
                        if (dist1 < dist2)
                        {
                            normal = results[0].normal;
                            center = results[0].point;
                        }
                        else
                        {
                            normal = results[1].normal;
                            center = results[1].point;
                        }
                    }

                    break;

                case 1:
                    normal = results[0].normal;
                    center = results[0].point;
                    break;

                case 0:
                    normal = Vector3.up;
                    center = transform.position + (Vector3.down * PlayerInfo.totalLegLength);
                    break;
            }

            Debug.DrawLine(transform.position, hitN.point, Color.green);
            Debug.DrawLine(transform.position, hitS.point, Color.green);
            Debug.DrawLine(legR, hitE.point, Color.green);
            Debug.DrawLine(legL, hitW.point, Color.green);

            Vector3 hfwd = Vector3.ProjectOnPlane(transform.forward, normal);

            //fix for gimbal lock when trying to figure out hipspace on high slopes
            if (Vector3.Dot(transform.forward, normal) <= -0.9f)
                hfwd += Vector3.up;

            hipSpace.rotation = Quaternion.LookRotation(hfwd, normal);
            if (normal == Vector3.zero)
                normal = PlayerInfo.pelvis.forward;
            vFloor.forward = -normal;

            vFloor.position = center;

            float grounddist = Vector3.Distance(transform.position, vFloor.position) - PlayerInfo.totalLegLength;
            if (results.Count < 1)
                grounddist = PlayerInfo.airTime;
            PlayerInfo.groundDistance = grounddist;

            if (PlayerInfo.grounded && !PlayerInfo.climbing)
                PlayerInfo.airTime = 0;
            else
                PlayerInfo.airTime += Time.fixedDeltaTime;
        }

        private void CastWalk(Ray left, Ray right, out RaycastHit hitL, out RaycastHit hitR, float radius = -1)
        {
            if (radius == -1)
                radius = PlayerInfo.vismodelRef.legWidth;

            bool hitLeft1 = Physics.SphereCast(left.origin, radius, left.direction, out RaycastHit hitInfoLeft, 1000, Shortcuts.geometryMask);
            bool hitRight1 = Physics.SphereCast(right.origin, radius, right.direction, out RaycastHit hitInfoRight, 1000, Shortcuts.geometryMask);
            hitL = hitInfoLeft;
            hitR = hitInfoRight;
            if (hitLeft1)
            {
                Vector3 leftup = hitL.point;
                leftup.y = transform.position.y;
                bool hitLeft2 = Physics.SphereCast(leftup, radius * 0.25f, Vector3.down, out RaycastHit hitInfoLeft2, 1000, Shortcuts.geometryMask);
                if (hitLeft2 && hitInfoLeft2.point.y > hitL.point.y)
                    hitL = hitInfoLeft2;
            }
            if (hitRight1)
            {
                Vector3 rightup = hitR.point;
                rightup.y = transform.position.y;
                bool hitRight2 = Physics.SphereCast(rightup, radius * 0.25f, Vector3.down, out RaycastHit hitInfoRight2, 1000, Shortcuts.geometryMask);
                if (hitRight2 && hitInfoRight2.point.y > hitR.point.y)
                    hitR = hitInfoRight2;
            }
        }
    }
}
