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
    public float timescale //quick access to timescale
    {
        get { return Time.timeScale; }
        set { Time.timeScale = value; }
    }

    [Header("References")]
    private MovementSettings m_movementSettings;
    public MovementSettings movementSettings
    {
        get { return m_movementSettings; }
        set { m_movementSettings = value; CopyValues(); } //copy values any time the movement settings are changed
    }

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
        legWidthMult, 
        ratiomult, 
        ratiofreezethreshold, 
        hipRotationSpeed, 
        down, 
        maxforcemult, 
        forcesmoothness, 
        maxProbeOffset, 
        probeCutoffHeight,
        maxlegmult, 
        crouchmult, 
        jumpmult, 
        jumpforcemult, 
        probemult, 
        movespeed, 
        friction, 
        slidemult, 
        wallruntilt, 
        airdownmult, 
        moveflatprobemult, 
        probeXminimumOffset, 
        probeZminimumOffset, 
        hipspaceMaxRot, 
        airtimemult, 
        moveupmult, 
        movedownmult, 
        movedownclamp, 
        hipspacesmoothness, 
        moveburst, 
        crawlthreshold, 
        crawlspeed, 
        crouchspeed, 
        crawlmult, 
        flightforce, 
        flightdrag, 
        sprintmult, 
        directionaljumpmult, 
        jumptilt, 
        slidepushforce, 
        climbtilt, 
        walkthreshold, 
        maxAirAccel, 
        ratioBySpeed, 
        jumpgrav, 
        fallgrav, 
        airmove, 
        airdrag;

    //internal variables
    private RaycastHit[] spherecastHitBufferL = new RaycastHit[100];
    private RaycastHit[] spherecastHitBufferR = new RaycastHit[100];
    private Transform animModelHips;
    private Transform animModelLFoot;
    private Transform animModelRFoot;
    private Rigidbody rb;
    private float legLength;
    private float animPhase = 0;
    private float currentratio = 1;

    private void Awake()
    {
        movementSettings = defaultMovementSettings;
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
        PlayerInfo.pelviscollision = false;

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
        Vector3 posHip = visModel.anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
        Vector3 posKnee = visModel.anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position;
        Vector3 posFoot = visModel.anim.GetBoneTransform(HumanBodyBones.LeftFoot).position;
        float thighLength = Vector3.Distance(posHip, posKnee);
        float calfLength = Vector3.Distance(posKnee, posFoot);
        legLength = thighLength + calfLength;
        legLength *= maxlegmult;
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
        bool crawling = inputBellyslide && velflathip.magnitude < crawlthreshold && inputCrouch;
        inputBellyslide &= !crawling;
        inputBackslide &= !crawling;
        bool doGroundLogic = PlayerInfo.grounded && PlayerInfo.footsurface.y >= -0.001f;
        
        PlayerInfo.crawling = crawling;

        if (!PlayerInfo.grounded)
        {
            if (!inputJump) rb.AddForce(Vector3.up * (fallgrav - Physics.gravity.y), ForceMode.Acceleration);
            else
                rb.AddForce(Vector3.up * (jumpgrav - Physics.gravity.y), ForceMode.Acceleration);

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            PlayerInfo.flying = true;
            rb.useGravity = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            PlayerInfo.flying = false;
            rb.useGravity = true;
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
                object value = field.GetValue(movementSettings);
                destField.SetValue(this, value);
            }
        }
        PlayerInfo.movespeed = movespeed;
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
            rb.AddTorque(Vector3.up * angle * hipRotationSpeed * Time.fixedDeltaTime);
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

        // Apply air drag
        if (airdrag != 1)
        {
            float vely = rb.velocity.y;
            rb.velocity *= airdrag;
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
        Vector3 velocityDifference = (flattened * airmove) - projectedVelocity;

        // Cap the magnitude of the velocity difference
        if (velflat.magnitude > maxAirAccel)
        {
            float angle = Vector3.Angle(velflat, flattened) / 180;
            velocityDifference = velocityDifference.normalized * airmove * angle;
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

        float yclampmin = 0;
        float yclampmax = 0;

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
        rb.velocity *= (1 - flightdrag);
        rb.AddForce(moveFlat * flightforce, ForceMode.Acceleration);
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

        float t = hipSpace.TransformVector(moveFlat).normalized.y;

        if (PlayerInfo.climbing)
            t = climbtilt;
        else if (inputJump)
            t = jumptilt;
        else
            t *= wallruntilt;

        Vector3 pushdir = Vector3.Lerp(Vector3.up, PlayerInfo.footsurface, t);

        float downness = Mathf.Clamp01(1 - hipSpace.up.y) * Mathf.Abs(hipSpace.up.x);
        float movedownamount = Mathf.Clamp((hipSpace.TransformVector(moveFlat).normalized.y * rb.velocity.magnitude), -movedownclamp, 0);
        movedownamount *= movedownmult;
        movedownamount -= downness;

        float headdist = PlayerInfo.playermodelAnim.GetBoneTransform(HumanBodyBones.Head).position.y - physHead.transform.position.y;
        headdist = Mathf.Clamp(headdist, 0, Mathf.Infinity);
        float legclamp = legLength - (headdist * 2.5f);

        float moveupamount = Mathf.Clamp01(hipSpace.TransformVector(moveFlat).y);
        moveupamount *= moveupmult;

        float legadjust = (animModelHips.position.y - animModelFootSelection().position.y) / legLength;
        if (PlayerInfo.climbing && !inputJump)
            legadjust = 0;

        if (PlayerInfo.crawling)
            legadjust = crawlmult;
        else if (inputJump)
        {
            legadjust *= jumpmult;
            rb.AddForce(Vector3.up * (jumpgrav - Physics.gravity.y));
        }
        else if (inputCrouch)
        {
            legadjust *= crouchmult;
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

        float forceadjust = maxforcemult;
        if (LucidInputValueShortcuts.jump && relativeheight <= maxlegmult)
            forceadjust *= jumpforcemult;

        float currentY = rb.velocity.y;
        float targetY = Mathf.LerpUnclamped(forceadjust, 0, heightratio);
        float diff = targetY - currentY;

        Vector3 calc = pushdir * (-Physics.gravity.y + (diff * forcesmoothness));
        NrmSlide(calc * rb.mass, out Vector3 slide, out float nrm);
        slide = -slide;

        Vector3 dir = hipSpace.TransformVector(moveFlat);
        if (dir.magnitude == 0)
            dir = -rb.velocity;
        Vector3 current = rb.velocity * rb.mass;
        NrmSlide(current, out Vector3 velslide, out float velnrm);
        slide += velslide * slidemult * (Vector3.Angle(rb.velocity, dir) / 180);
        nrm += velnrm;

        PlayerInfo.alignment = (Vector3.Angle(rb.velocity, dir) / 180) * Mathf.Clamp01(rb.velocity.magnitude);

        float moveadjust = movespeed;
        if (PlayerInfo.crawling)
            moveadjust = crawlspeed;
        else if (inputCrouch)
            moveadjust = crouchspeed;
        if (!inputSprint && Mathf.Abs(moveFlat.x) < walkthreshold)
            moveadjust *= sprintmult;

        float diffmag = 1 - Mathf.Clamp01(rb.velocity.magnitude / moveadjust);
        moveadjust *= 1 + (diffmag * moveburst);

        float jumpadjust = inputJump ? directionaljumpmult : 1;
        Vector3 movetarget = hipSpace.TransformVector(moveFlat) * moveadjust * jumpadjust;
        Vector3 relativevel = hipSpace.InverseTransformVector(rb.velocity);
        relativevel.y = 0;
        relativevel = hipSpace.TransformVector(relativevel);
        Vector3 movediff = movetarget - relativevel;
        Vector3 surfaction = -movediff * rb.mass;
        slide += surfaction;

        nrm = Mathf.Clamp(nrm, 0, Mathf.Infinity);

        float fmax = Mathf.Sqrt(nrm) * rb.mass * friction;
        PlayerInfo.traction = Mathf.Clamp01(fmax / slide.magnitude);
        Vector3 pushcalc = nrm * PlayerInfo.footsurface;
        Vector3 slidecalc = Vector3.ClampMagnitude(-slide, fmax);
        rb.AddForce(slidecalc + pushcalc, ForceMode.Force);
        PlayerInfo.currentslide = slidecalc;
        PlayerInfo.currentpush = pushcalc;
    }

    private Transform animModelFootSelection()
    {
        if (animPhase > 0.5f)
            return animModelRFoot;
        else
            return animModelLFoot;
    }

    public void OnPhysCollisionStay(Collision c)
    {
        PlayerInfo.physCollision = true;
    }

    public void OnPhysCollisionExit(Collision c)
    {
        PlayerInfo.physCollision = false;
    }

    private void OnCollisionStay(Collision collision)
    {
        PlayerInfo.pelviscollision = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        PlayerInfo.pelviscollision = false;
    }

    private void SlidePush()
    {
        Vector3 diffL = legSpaceL.position - PlayerInfo.IK_LF.position;
        Vector3 diffR = legSpaceR.position - PlayerInfo.IK_RF.position;
        Vector3 avg = (diffL + diffR) / 2;
        float strength = 1 - Mathf.Clamp01(avg.magnitude / legLength);
        avg = avg.normalized * strength;

        rb.AddForce(avg * slidepushforce, ForceMode.Acceleration);
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

        bool currentright = animPhase < 0.5f;

        float stepphase = 0.5f - animPhase;
        stepphase = Mathf.Abs(stepphase) * 2;
        PlayerInfo.stepphase = stepphase;

        Vector3 willflat = (hipSpace.TransformVector(moveFlat) * (movespeed / 2));
        willflat.y = 0;

        Vector3 velflat = rb.velocity;
        velflat.y = 0;

        Vector3 velrelative = transform.InverseTransformVector(velflat);

        Vector3 legR = legSpaceR.position;
        legR += legSpaceR.up * 0.1f;
        Vector3 legL = legSpaceL.position;
        legL += legSpaceL.up * 0.1f;

        float downadjust = -down;
        if (!PlayerInfo.grounded)
            downadjust = -down * airdownmult;
        downadjust += transform.position.y;

        Vector3 veladjust = velrelative;
        if (!PlayerInfo.grounded)
            veladjust = moveFlat * moveflatprobemult;

        Vector3 probeN = transform.position + (transform.forward * Mathf.Clamp(Mathf.Abs(veladjust.z) * probemult, probeZminimumOffset, Mathf.Infinity));
        probeN.y = (Vector3.up * downadjust).y;
        Vector3 probeS = transform.position - (transform.forward * Mathf.Clamp(Mathf.Abs(veladjust.z) * probemult, probeZminimumOffset, Mathf.Infinity));
        probeS.y = (Vector3.up * downadjust).y;
        Vector3 probeE = transform.position + (transform.right * Mathf.Clamp(Mathf.Abs(veladjust.x) * probemult, probeXminimumOffset, Mathf.Infinity));
        probeE.y = (Vector3.up * downadjust).y;
        Vector3 probeW = transform.position - (transform.right * Mathf.Clamp(Mathf.Abs(veladjust.x) * probemult, probeXminimumOffset, Mathf.Infinity));
        probeW.y = (Vector3.up * downadjust).y;

        //probe Z
        Ray N = new Ray(transform.position, probeN - transform.position);
        Ray S = new Ray(transform.position, probeS - transform.position);
        CastWalk(N, S, out RaycastHit hitN, out RaycastHit hitS, -1);

        //probe X
        Ray E = new Ray(legR, probeE - transform.position);
        Ray W = new Ray(legL, probeW - transform.position);
        CastWalk(E, W, out RaycastHit hitE, out RaycastHit hitW, -1);

        float diffZ = Mathf.Abs(hitN.point.y - hitS.point.y);
        float diffX = Mathf.Abs(hitW.point.y - hitE.point.y);

        List<RaycastHit> results = new List<RaycastHit>();
        if (diffZ < diffX)
        {
            if (hitN.point.sqrMagnitude > float.Epsilon && hitN.distance < maxProbeOffset)
                results.Add(hitN);
            if (hitS.point.sqrMagnitude > float.Epsilon && hitS.distance < maxProbeOffset)
                results.Add(hitS);
            if (hitE.point.sqrMagnitude > float.Epsilon && hitE.distance < maxProbeOffset)
                results.Add(hitE);
            if (hitW.point.sqrMagnitude > float.Epsilon && hitW.distance < maxProbeOffset)
                results.Add(hitW);
        }
        else
        {
            if (hitE.point.sqrMagnitude > float.Epsilon && hitE.distance < maxProbeOffset)
                results.Add(hitE);
            if (hitW.point.sqrMagnitude > float.Epsilon && hitW.distance < maxProbeOffset)
                results.Add(hitW);
            if (hitN.point.sqrMagnitude > float.Epsilon && hitN.distance < maxProbeOffset)
                results.Add(hitN);
            if (hitS.point.sqrMagnitude > float.Epsilon && hitS.distance < maxProbeOffset)
                results.Add(hitS);
        }

        Vector3 normal = Vector3.zero;
        Vector3 center = Vector3.zero;

        switch (results.Count)
        {
            case 4:
                Vector3 a = results[0].point;
                Vector3 b = results[1].point;
                Vector3 c = Vector3.zero;

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
        if (Vector3.Dot(transform.forward, normal) <= hipspaceMaxRot)
            hfwd += Vector3.up;
        Quaternion q1 = hipSpace.rotation;
        Quaternion q2 = Quaternion.LookRotation(hfwd, normal);
        Quaternion q3 = Quaternion.RotateTowards(q1, q2, Quaternion.Angle(q1, q2) * (1 - hipspacesmoothness));
        hipSpace.rotation = q3;

        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, hipSpace.up);

        vFloor.position = center;
        vFloor.forward = -normal;

        float grounddist = Vector3.Distance(transform.position, vFloor.position) - legLength;
        if (results.Count < 1)
            grounddist = Mathf.Infinity;
        PlayerInfo.grounddist = grounddist;

        float currentratiomult = ratiomult * (1 + (velflat.magnitude * ratioBySpeed));

        float ratio = currentratio / currentratiomult;
        ratio = Mathf.Clamp(ratio, ratiofreezethreshold, 1);

        if (PlayerInfo.grounded && !PlayerInfo.climbing)
            PlayerInfo.airtime = 0;
        else
            PlayerInfo.airtime += Time.fixedDeltaTime;

        if (velflat.magnitude > 0.1f)
        {
            float add = (Time.fixedDeltaTime * 0.5f) / ratio;
            add /= 1 + (PlayerInfo.airtime * airtimemult);
            animPhase += add;
            animPhase %= 1;
        }
        PlayerInfo.animphase = animPhase;
    }

    private void CastWalk(Ray left, Ray right, out RaycastHit hitL, out RaycastHit hitR, float radius = -1)
    {
        if (radius == -1)
            radius = legWidth * legWidthMult;

        float leftslope = left.direction.y;
        float maxdistL = probeCutoffHeight / leftslope;
        float rightslope = right.direction.y;
        float maxdistR = probeCutoffHeight / rightslope;

        int hitLeft1 = Physics.SphereCastNonAlloc(left.origin, radius, left.direction, spherecastHitBufferL, maxdistL, Shortcuts.geometryMask);
        int hitRight1 = Physics.SphereCastNonAlloc(right.origin, radius, right.direction, spherecastHitBufferR, maxdistR, Shortcuts.geometryMask);
        hitL = spherecastHitBufferL[0];
        hitR = spherecastHitBufferR[0];
        if (hitLeft1 > 0)
        {
            Vector3 leftup = hitL.point;
            leftup.y = transform.position.y;
            bool hitLeft2 = Physics.SphereCast(leftup, radius * 0.25f, Vector3.down, out RaycastHit hitInfoLeft2, 1000, Shortcuts.geometryMask);
            if (hitLeft2 && hitInfoLeft2.point.y > hitL.point.y)
                hitL = hitInfoLeft2;
        }
        if (hitRight1 > 0)
        {
            Vector3 rightup = hitR.point;
            rightup.y = transform.position.y;
            bool hitRight2 = Physics.SphereCast(rightup, radius * 0.25f, Vector3.down, out RaycastHit hitInfoRight2, 1000, Shortcuts.geometryMask);
            if (hitRight2 && hitInfoRight2.point.y > hitR.point.y)
                hitR = hitInfoRight2;
        }
    }
}

