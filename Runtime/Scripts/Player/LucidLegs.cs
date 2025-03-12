using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

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

public class LucidLegs : MonoBehaviour
{
    public float Timescale //quick access to timescale
    {
        get { return Time.timeScale; }
        set { Time.timeScale = value; }
    }

    [Header("References")]
    private MovementSettings m_movementSettings;
    public MovementSettings MovementSettings
    {
        get { return m_movementSettings; }
        set { m_movementSettings = value; CopyValues(); } //copy values any time the movement settings are changed
    }

    public bool autoSprint = true;
    public bool overrideSprint = false;

    [HideInInspector]
    public bool sprintOverride = false;

    [SerializeField] Transform legSpaceL;
    [SerializeField] Transform legSpaceR;
    [SerializeField] Transform hipSpace;
    [SerializeField] Transform footSpace;
    [SerializeField] Transform vFloor;
    [SerializeField] BoxCollider physBody;
    [SerializeField] SphereCollider physHead;
    [SerializeField] MovementSettings defaultMovementSettings;

    //parameters copied from movementsettings
    private float
        legWidth,
        ratioScale,
        ratioFreezeThreshold,
        pelvisRotationSpeed,
        probeDepth,
        maxForceScale,
        forceSmoothness,
        maxProbeOffset,
        probeCutoffHeight,
        jumpHeightScale,
        jumpForceScale,
        probeScale,
        moveSpeed,
        friction,
        footSlideStrength,
        slopeTilt,
        probeDepthByFall,
        probeXMinimumOffset,
        probeZMinimumOffset,
        hipSpaceMaxRotation,
        dampAnimPhaseByAirtime,
        targetHeightByPositiveSlope,
        targetHeightByNegativeSlope,
        targetHeightByNegativeSlopeClamp,
        hipSpaceRotationSmoothness,
        moveBurst,
        maxCrawlSpeed,
        moveSpeedCrawling,
        moveSpeedCrouched,
        crawlHeight,
        flightSpeed,
        flightDrag,
        sprintScale,
        directionalJumpStrength,
        jumpTilt,
        slidePushStrength,
        climbTilt,
        strafeWalkAngularThreshold,
        strafeWalkSpeedMult,
        maxAirAcceleration,
        scaleRatioBySpeed,
        jumpGravity,
        fallGravity,
        aerialMovementSpeed,
        aerialDrag,
        slidePushAngleThreshold,
        maxSlopeDefault,
        maxSlopeByYVelocity,
        surfaceMagnetismBySlope
        = 0;

    //internal variables
    private Transform animModelHips;
    private Transform animModelLFoot;
    private Transform animModelRFoot;
    private Rigidbody rb;
    private Vector3 bodyCollisionNrm;
    private float legLength;
    private float hipLength;
    private float animPhase = 0;
    private float currentratio = 1;

    private void Awake()
    {
        MovementSettings = defaultMovementSettings;
        rb = GetComponent<Rigidbody>();
        SetPlayerInfoReferences();
    }

    private void OnEnable()
    {
        PlayerInfo.OnAssignVismodel.AddListener(OnAssignVismodel);
        PlayerInfo.OnAnimModellInitialized.AddListener(OnAnimModelInitialized);
    }

    private void OnDisable()
    {
        PlayerInfo.flying = false; //this is a fix for a bug occurring when you switch scenes while in a flight zone - i may eventually have a function to reset all temporary values in playerinfo on scene change
        PlayerInfo.pelvisCollision = false;

        PlayerInfo.OnAssignVismodel.RemoveListener(OnAssignVismodel);
        PlayerInfo.OnAnimModellInitialized.RemoveListener(OnAnimModelInitialized);
    }

    public void OnAssignVismodel(LucidVismodel visModel)
    {
        CalculateLegLength(visModel);
        ConfigurePhysModel(visModel);
    }

    //uses approximate bone lengths from vismodel to determine total outstretched leg length
    private void CalculateLegLength(LucidVismodel visModel)
    {
        float thighLength = visModel.anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg).localPosition.magnitude;
        float calfLength = visModel.anim.GetBoneTransform(HumanBodyBones.LeftFoot).localPosition.magnitude;
        legLength = thighLength + calfLength;

        Vector3 hips = visModel.anim.GetBoneTransform(HumanBodyBones.Hips).position;
        Vector3 thigh = visModel.anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
        hipLength = hips.y - thigh.y;
    }

    //copies physmodel configuration from vismodel
    private void ConfigurePhysModel(LucidVismodel visModel)
    {
        physBody.size = visModel.bodyCollider.size;
        physBody.center = visModel.bodyCollider.center;
        physHead.radius = visModel.headCollider.radius;
        physHead.center = visModel.headCollider.center;
        ConfigurableJoint hcj = physHead.GetComponent<ConfigurableJoint>();
        Vector3 headoffset = visModel.headCollider.transform.position - visModel.bodyCollider.transform.position;
        hcj.connectedAnchor = headoffset;
    }

    public void OnAnimModelInitialized()
    {
        animModelHips = PlayerInfo.playermodelAnim.GetBoneTransform(HumanBodyBones.Hips);
        animModelLFoot = PlayerInfo.playermodelAnim.GetBoneTransform(HumanBodyBones.LeftFoot);
        animModelRFoot = PlayerInfo.playermodelAnim.GetBoneTransform(HumanBodyBones.RightFoot);
    }

    private void FixedUpdate()
    {
        if (rb.isKinematic || PlayerInfo.vismodelRef == null || !PlayerInfo.animModelInitialized) return; //this could technically be optimized by delegating it to a bool that only updates when isKinematic is updated, but that's too much work and i don't think this is much of a perf hit

        PseudoWalk();
        RotationLogic();

        Vector3 velflathip = hipSpace.InverseTransformVector(rb.velocity);
        velflathip.y = 0;

        //all of these need to eventually be delegated to bools
        bool inputBellyslide = LucidInputValueShortcuts.bslide;
        bool inputBackslide = LucidInputValueShortcuts.slide;
        bool inputCrouch = LucidInputValueShortcuts.crouch;
        bool inputJump = LucidInputValueShortcuts.jump;
        bool inputSprint = LucidInputValueShortcuts.sprint;
        if (overrideSprint)
            inputSprint = sprintOverride;
        else if (autoSprint)
            inputSprint = !inputSprint;
        bool crawling = inputBellyslide && inputCrouch && (velflathip.magnitude < maxCrawlSpeed);
        inputBellyslide &= !crawling;
        inputBackslide &= !crawling;
        bool doGroundLogic = PlayerInfo.grounded && PlayerInfo.footSurface.y >= -0.001f;
        
        PlayerInfo.crawling = crawling;

        if (!PlayerInfo.grounded)
        {
            if (!inputJump) rb.AddForce(Vector3.up * (fallGravity - Physics.gravity.y), ForceMode.Acceleration);
            else
                rb.AddForce(Vector3.up * (jumpGravity - Physics.gravity.y), ForceMode.Acceleration);

            AirCalc();
        }
        else if (!PlayerInfo.flying && doGroundLogic)
        {
            if (inputBellyslide || inputBackslide)
                SlidePush();
            else
                LegPush(inputCrouch, inputJump, inputSprint);
        }

        if (PlayerInfo.flying)
            FlightCalc();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            PlayerInfo.flying = false;
            rb.useGravity = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == 7)
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
        PlayerInfo.hipspace = hipSpace;
        PlayerInfo.footspace = footSpace;
        PlayerInfo.legspaceL = legSpaceL;
        PlayerInfo.legspaceR = legSpaceR;
        PlayerInfo.physBody = physBody;
        PlayerInfo.physBodyRB = physBody.GetComponent<Rigidbody>();
        PlayerInfo.physHead = physHead;
        PlayerInfo.physHeadRB = physHead.GetComponent<Rigidbody>();
    }

    //copies values from movement settings to this script, using property names to match values
    public void CopyValues()
    {
        Type TMoveSettings = typeof(MovementSettings);
        Type TLegs = typeof(LucidLegs);
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
        if (aerialDrag != 1)
        {
            float vely = rb.velocity.y;
            rb.velocity *= aerialDrag;
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
        }

        // Apply the force
        rb.AddForce(velocityDifference, ForceMode.Acceleration);
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
            yclampmin = 2;
        }
        else if (crouch)
        {
            yclampmax = -2;
            yclampmin = -Mathf.Infinity;
        }
        else
        {
            yclampmax = Mathf.Infinity;
            yclampmin = -Mathf.Infinity;
        }
        moveFlat.y = Mathf.Clamp(moveFlat.y, yclampmin, yclampmax);
        rb.velocity *= (1 - flightDrag);
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
        else if (inputJump)
            t = jumpTilt;
        else
            t *= slopeTilt;

        Vector3 pushdir = Vector3.Lerp(Vector3.up, PlayerInfo.footSurface, t);

        float downness = Mathf.Clamp01(1 - footSpace.up.y) * Mathf.Abs(footSpace.up.x);
        float movedownamount = Mathf.Clamp((footSpace.TransformVector(moveFlat).normalized.y * rb.velocity.magnitude), -targetHeightByNegativeSlopeClamp, 0);
        movedownamount *= targetHeightByNegativeSlope;
        movedownamount -= downness;

        float headdist = PlayerInfo.playermodelAnim.GetBoneTransform(HumanBodyBones.Head).position.y - physHead.transform.position.y;
        headdist = Mathf.Clamp(headdist, 0, Mathf.Infinity);
        float legclamp = ((legLength + hipLength) * PlayerInfo.vismodelRef.maxLegScale) - (headdist * 2.5f);

        float moveupamount = Mathf.Clamp01(footSpace.TransformVector(moveFlat).y);
        moveupamount *= targetHeightByPositiveSlope;

        float legadjust = (animModelHips.position.y - AnimModelFootSelection().position.y) / ((legLength + hipLength) * PlayerInfo.vismodelRef.maxLegScale);
        if (PlayerInfo.climbing && !inputJump)
            legadjust = 0;

        if (PlayerInfo.crawling)
            legadjust = crawlHeight;
        else if (inputJump)
        {
            legadjust *= jumpHeightScale;
            rb.AddForce(Vector3.up * (jumpGravity - Physics.gravity.y));
        }
        else
        {
            legadjust *= 1 + movedownamount + moveupamount;
        }

        if(PlayerInfo.physCollision)
            legadjust = Mathf.Clamp(legadjust, 0, legclamp);

        float relativeheight = hipSpace.position.y - footSpace.position.y;
        currentratio = relativeheight;
        float heightratio = relativeheight / legadjust;
        heightratio = Mathf.Clamp(heightratio, 0, 2);

        float forceadjust = maxForceScale;
        if (LucidInputValueShortcuts.jump && relativeheight <= PlayerInfo.vismodelRef.maxLegScale)
            forceadjust *= jumpForceScale;

        float currentY = rb.velocity.y;
        float targetY = Mathf.LerpUnclamped(forceadjust, 0, heightratio);
        float diff = targetY - currentY;

        Vector3 calc = pushdir * (-Physics.gravity.y + (diff * forceSmoothness) + pointVelocity.y);
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

        PlayerInfo.alignment = (Vector3.Angle(rb.velocity, dir) / 180) * Mathf.Clamp01(rb.velocity.magnitude);

        float moveadjust = moveSpeed;
        if (PlayerInfo.crawling)
            moveadjust = moveSpeedCrawling;
        else if (inputCrouch)
            moveadjust = moveSpeedCrouched;
        if (inputSprint)
            moveadjust *= sprintScale;
        if (Mathf.Abs(moveFlat.x) > strafeWalkAngularThreshold)
            moveadjust *= strafeWalkSpeedMult;

        float diffmag = 1 - Mathf.Clamp01(rb.velocity.magnitude / moveadjust);
        moveadjust *= 1 + (diffmag * moveBurst);

        float jumpadjust = inputJump ? directionalJumpStrength : 1;
        Vector3 movetarget = jumpadjust * moveadjust * hipSpace.TransformVector(moveFlat);
        Vector3 relativevel = hipSpace.InverseTransformVector(rb.velocity);
        relativevel.y = 0;
        relativevel = hipSpace.TransformVector(relativevel);
        Vector3 movediff = movetarget - relativevel;
        Vector3 surfaction = -movediff;
        slide += surfaction * rb.mass;

        nrm = Mathf.Clamp(nrm, 0, Mathf.Infinity);

        float fmax = Mathf.Sqrt(nrm) * rb.mass * friction;
        PlayerInfo.traction = Mathf.Clamp01(fmax / slide.magnitude);
        Vector3 pushcalc = nrm * PlayerInfo.footSurface;
        Vector3 slidecalc = Vector3.ClampMagnitude(-slide, fmax);
        PlayerInfo.currentFootSlide = slidecalc;
        PlayerInfo.currentLegPush = pushcalc;

        Vector3 objectforce = -(slidecalc + pushcalc);

        rb.AddForce(slidecalc + pushcalc, ForceMode.Force);

        if (isRight && PlayerInfo.connectedRB_RF != null)
        {
            PlayerInfo.connectedRB_RF.AddForceAtPosition(objectforce, PlayerInfo.IK_RF.position, ForceMode.Force);
        }
        if (!isRight && PlayerInfo.connectedRB_LF != null)
            PlayerInfo.connectedRB_LF.AddForceAtPosition(objectforce, PlayerInfo.IK_LF.position, ForceMode.Force);
    }

    private Transform AnimModelFootSelection()
    {
        if (animPhase > 0.5f)
            return animModelRFoot;
        else
            return animModelLFoot;
    }

    public void OnPhysCollisionStay(Collision c)
    {
        PlayerInfo.physCollision = true;
        bodyCollisionNrm = c.contacts[0].normal;
    }

    public void OnPhysCollisionExit(Collision c)
    {
        PlayerInfo.physCollision = false;
    }

    private void OnCollisionStay(Collision collision)
    {
        PlayerInfo.pelvisCollision = true;
        bodyCollisionNrm = collision.contacts[0].normal;
    }

    private void OnCollisionExit(Collision collision)
    {
        PlayerInfo.pelvisCollision = false;
    }

    private void SlidePush()
    {
        PlayerInfo.surfaceAngle = Vector3.Angle(bodyCollisionNrm, PlayerInfo.footspace.up);

        if (PlayerInfo.surfaceAngle < slidePushAngleThreshold)
            return;

        Vector3 diffL = legSpaceL.position - PlayerInfo.IK_LF.position;
        Vector3 diffR = legSpaceR.position - PlayerInfo.IK_RF.position;
        Vector3 avg = (diffL + diffR) / 2;
        float strength = 1 - Mathf.Clamp01(avg.magnitude / legLength);
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

        float stepphase = 0.5f - animPhase;
        stepphase = Mathf.Abs(stepphase) * 2;
        PlayerInfo.stepPhase = stepphase;

        Vector3 willflat = (PlayerInfo.pelvis.TransformVector(moveFlat) * (moveSpeed / 2));
        willflat.y = 0;

        Vector3 velflat = rb.velocity;
        velflat.y = 0;

        Vector3 legR = legSpaceR.position;
        legR += legSpaceR.up * 0.1f;
        Vector3 legL = legSpaceL.position;
        legL += legSpaceL.up * 0.1f;

        float depthadjust = (probeDepthByFall * rb.velocity.y);
        depthadjust = Mathf.Clamp(depthadjust, 0, float.MaxValue);

        float downadjust = -probeDepth;
        if (!PlayerInfo.grounded)
            downadjust = -probeDepth * depthadjust;
        downadjust += transform.position.y;

        Vector3 probeN = transform.position + (transform.forward * Mathf.Clamp(Mathf.Abs(moveFlat.z) * probeScale, probeZMinimumOffset, Mathf.Infinity));
        probeN.y = (Vector3.up * downadjust).y;
        Vector3 probeS = transform.position - (transform.forward * Mathf.Clamp(Mathf.Abs(moveFlat.z) * probeScale, probeZMinimumOffset, Mathf.Infinity));
        probeS.y = (Vector3.up * downadjust).y;
        Vector3 probeE = transform.position + (transform.right * Mathf.Clamp(Mathf.Abs(moveFlat.x) * probeScale, probeXMinimumOffset, Mathf.Infinity));
        probeE.y = (Vector3.up * downadjust).y;
        Vector3 probeW = transform.position - (transform.right * Mathf.Clamp(Mathf.Abs(moveFlat.x) * probeScale, probeXMinimumOffset, Mathf.Infinity));
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

        float minNrmY = 1 - (maxSlopeDefault + (Mathf.Clamp(rb.velocity.y, 0, 1 / maxSlopeByYVelocity) * maxSlopeByYVelocity));

        List<RaycastHit> results = new();
        if (diffZ < diffX)
        {
            if (hitN.point.sqrMagnitude > float.Epsilon && hitN.distance < maxProbeOffset && hitN.normal.y > minNrmY)
                results.Add(hitN);
            if (hitS.point.sqrMagnitude > float.Epsilon && hitS.distance < maxProbeOffset && hitS.normal.y > minNrmY)
                results.Add(hitS);
            if (hitE.point.sqrMagnitude > float.Epsilon && hitE.distance < maxProbeOffset && hitE.normal.y > minNrmY)
                results.Add(hitE);
            if (hitW.point.sqrMagnitude > float.Epsilon && hitW.distance < maxProbeOffset && hitW.normal.y > minNrmY)
                results.Add(hitW);
        }
        else
        {
            if (hitE.point.sqrMagnitude > float.Epsilon && hitE.distance < maxProbeOffset && hitE.normal.y > minNrmY)
                results.Add(hitE);
            if (hitW.point.sqrMagnitude > float.Epsilon && hitW.distance < maxProbeOffset && hitW.normal.y > minNrmY)
                results.Add(hitW);
            if (hitN.point.sqrMagnitude > float.Epsilon && hitN.distance < maxProbeOffset && hitN.normal.y > minNrmY)
                results.Add(hitN);
            if (hitS.point.sqrMagnitude > float.Epsilon && hitS.distance < maxProbeOffset && hitS.normal.y > minNrmY)
                results.Add(hitS);
        }

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
                if(dir.sqrMagnitude > float.Epsilon)
                {
                    Vector3 diff1 = results[0].point - transform.position;
                    Vector3 diff2 = results[1].point - transform.position;
                    float diffangle1 = Vector3.Angle(dir, diff1);
                    float diffangle2 = Vector3.Angle(dir, diff2);
                    if (diffangle1 < diffangle2)
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
                    if(dist1 < dist2)
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
                center = transform.position + (Vector3.down * legLength);
                break;
        }

        if (Vector3.Distance(transform.position, hitN.point) < maxProbeOffset)
            Debug.DrawLine(transform.position, hitN.point, Color.green);
        if (Vector3.Distance(transform.position, hitS.point) < maxProbeOffset)
            Debug.DrawLine(transform.position, hitS.point, Color.green);
        if (Vector3.Distance(legR, hitE.point) < maxProbeOffset)
            Debug.DrawLine(legR, hitE.point, Color.green);
        if (Vector3.Distance(legL, hitW.point) < maxProbeOffset)
            Debug.DrawLine(legL, hitW.point, Color.green);

        Vector3 hfwd = Vector3.ProjectOnPlane(transform.forward, normal);
        if (Vector3.Dot(transform.forward, normal) <= hipSpaceMaxRotation)
            hfwd += Vector3.up;
        Quaternion q1 = hipSpace.rotation;
        Quaternion q2 = Quaternion.LookRotation(hfwd, normal);
        float deltaQ = Quaternion.Angle(q1, q2);

        Quaternion q3 = Quaternion.RotateTowards(q1, q2, deltaQ * (1 - hipSpaceRotationSmoothness));

        hipSpace.rotation = q3;
        vFloor.forward = -normal;

        vFloor.position = center;

        float grounddist = Vector3.Distance(transform.position, vFloor.position) - legLength;
        if (results.Count < 1)
            grounddist = Mathf.Infinity;
        PlayerInfo.groundDistance = grounddist;

        float currentratiomult = ratioScale * (1 + (velflat.magnitude * scaleRatioBySpeed));

        float ratio = currentratio / currentratiomult;
        ratio = Mathf.Clamp(ratio, ratioFreezeThreshold, 1);

        if (PlayerInfo.grounded && !PlayerInfo.climbing)
            PlayerInfo.airTime = 0;
        else
            PlayerInfo.airTime += Time.fixedDeltaTime;

        if (velflat.magnitude > 0.1f)
        {
            float add = (Time.fixedDeltaTime * 0.5f) / ratio;
            add /= 1 + (PlayerInfo.airTime * dampAnimPhaseByAirtime);
            animPhase += add;
            animPhase %= 1;
        }
        PlayerInfo.animPhase = animPhase;
    }

    private void CastWalk(Ray left, Ray right, out RaycastHit hitL, out RaycastHit hitR, float radius = -1)
    {
        if (radius == -1)
            radius = legWidth;

        float leftslope = left.direction.y;
        float maxdistL = probeCutoffHeight / leftslope;
        float rightslope = right.direction.y;
        float maxdistR = probeCutoffHeight / rightslope;

        bool hitLeft1 = Physics.SphereCast(left.origin, radius, left.direction, out RaycastHit hitInfoLeft, maxdistL, Shortcuts.geometryMask);
        bool hitRight1 = Physics.SphereCast(right.origin, radius, right.direction, out RaycastHit hitInfoRight, maxdistR, Shortcuts.geometryMask);
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

