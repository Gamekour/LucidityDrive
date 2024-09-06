using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

class GFG : IComparer<RaycastHit>
{
    public int Compare(RaycastHit x, RaycastHit y)
    {
        if (x.point.y == 0 || y.point.y == 0)
        {
            return 0;
        }

        // CompareTo() method
        return x.point.y.CompareTo(y.point.y);

    }
}

public class LucidLegs : MonoBehaviour
{
    [Header("References")]
    private MovementSettings m_movementSettings;
    public MovementSettings movementSettings
    {
        get { return m_movementSettings; }
        set { m_movementSettings = value; CopyValues(); }
    }

    [SerializeField] Rigidbody rb;
    [SerializeField] Transform legspaceL;
    [SerializeField] Transform legspaceR;
    [SerializeField] Transform hipspace;
    [SerializeField] Transform footspace;
    [SerializeField] Transform vfloor;
    [SerializeField] Collider physHips;
    [SerializeField] Collider physHead;
    [SerializeField] MovementSettings defaultMovementSettings;

    //These values are copied from the Movement Settings
    private float legWidth;
    private float legWidthMult;
    private float ratiomult;
    private float ratiofreezethreshold;
    private float hipRotationSpeed;
    private float down;
    private float maxforcemult;
    private float forcesmoothness;
    private float maxProbeOffset;
    private float maxlegmult;
    private float crouchmult;
    private float jumpmult;
    private float jumpforcemult;
    private float probemult;
    private float movespeed;
    private float friction;
    private float slidemult;
    private float wallruntilt;
    private float airdownmult;
    private float moveflatprobemult;
    private float probeXminimumOffset;
    private float probeZminimumOffset;
    private float hipspaceMaxRot;
    private float airtimemult;
    private float moveupmult;
    private float movedownmult;
    private float movedownclamp;
    private float hipspacesmoothness;
    private float moveburst;
    private float crawlthreshold;
    private float crawlspeed;
    private float crouchspeed;
    private float crawlmult;
    private float flightforce;
    private float flightdrag;
    private float sprintmult;
    private float directionaljumpmult;
    private float jumptilt;
    private float slidepushforce;
    private float climbtilt;

    private float m_timescale;
    private float timescale
    {
        get { return m_timescale; }
        set { m_timescale = value; Time.timeScale = value; }
    }

    private float jumpgrav;
    private float fallgrav;
    private float airmove;
    private float airdrag;

    //internal variables
    private RaycastHit[] spherecastHitBufferL = new RaycastHit[100];
    private RaycastHit[] spherecastHitBufferR = new RaycastHit[100];
    private Transform animModelHips;
    private Transform animModelLFoot;
    private Transform animModelRFoot;
    private float legLength;
    private float animPhase = 0;
    private float currentratio = 1;
    private bool headcolliding = false;

    private void Awake()
    {
        movementSettings = defaultMovementSettings; //starts process of copying values
        rb = GetComponent<Rigidbody>();
        SetPlayerInfoReferences();
    }

    private void Start()
    {
        movementSettings = defaultMovementSettings;
        legLength = transform.position.y - transform.root.position.y; //automatically determine leg length based on height from root - will eventually be replaced by a better avatar-specific system
        animModelHips = PlayerInfo.playermodelAnim.GetBoneTransform(HumanBodyBones.Hips);
        animModelLFoot = PlayerInfo.playermodelAnim.GetBoneTransform(HumanBodyBones.LeftFoot);
        animModelRFoot = PlayerInfo.playermodelAnim.GetBoneTransform(HumanBodyBones.RightFoot);
    }

    private void OnDisable()
    {
        PlayerInfo.flying = false;
        //this is a fix for a bug occurring when you switch scenes while in a flight zone - i may eventually have a function to reset all temporary values in playerinfo on scene change
        PlayerInfo.pelviscollision = false;
    }

    private void FixedUpdate()
    {
        if (PlayerInfo.mainBody.isKinematic || PlayerInfo.vismodelRef == null) return; //this could technically be optimized by delegating it to a bool that only updates when isKinematic is updated, but that's too much work and i don't think this is much of a perf hit

        PseudoWalk();
        RotationLogic();

        Vector3 velflathip = PlayerInfo.hipspace.InverseTransformVector(rb.velocity);
        velflathip.y = 0;

        //all of these need to eventually be delegated to bools
        bool inputBellyslide = LucidInputValueShortcuts.crawl;
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
            if (!inputJump) rb.AddForce(Vector3.up * (fallgrav - Physics.gravity.y));
            else
                rb.AddForce(Vector3.up * (jumpgrav - Physics.gravity.y));

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
        PlayerInfo.hipspace = hipspace;
        PlayerInfo.footspace = footspace;
        PlayerInfo.legspaceL = legspaceL;
        PlayerInfo.legspaceR = legspaceR;
        PlayerInfo.physHips = physHips;
        PlayerInfo.physHipsRB = physHips.GetComponent<Rigidbody>();
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

        if (airdrag != 1)
        {
            float vely = rb.velocity.y;
            rb.velocity *= airdrag;
            Vector3 vel = rb.velocity;
            vel.y = vely;
            rb.velocity = vel;
        }

        rb.AddForce(flattened * airmove, ForceMode.Acceleration);
    }

    //handles movement while flying
    private void FlightCalc()
    {
        Vector2 moveVector = LucidInputValueShortcuts.movement;
        Vector3 moveFlat = Vector3.zero;
        moveFlat.x = moveVector.x;
        moveFlat.z = moveVector.y;
        moveFlat = PlayerInfo.head.TransformVector(moveFlat);
        float yclamp = LucidInputValueShortcuts.jump ? 2 : 0;
        moveFlat.y = Mathf.Clamp(moveFlat.y, yclamp, Mathf.Infinity);
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

        float t = PlayerInfo.hipspace.TransformVector(moveFlat).normalized.y;

        if (PlayerInfo.climbing)
            t = climbtilt;
        else if (inputJump)
            t = jumptilt;
        else
            t *= wallruntilt;

        Vector3 pushdir = Vector3.Lerp(Vector3.up, PlayerInfo.footsurface, t);

        float downness = Mathf.Clamp01(1 - PlayerInfo.hipspace.up.y) * Mathf.Abs(PlayerInfo.hipspace.up.x);
        float movedownamount = Mathf.Clamp((PlayerInfo.hipspace.TransformVector(moveFlat).normalized.y * rb.velocity.magnitude), -movedownclamp, 0);
        movedownamount *= movedownmult;
        movedownamount -= downness;

        float headdist = PlayerInfo.playermodelAnim.GetBoneTransform(HumanBodyBones.Head).position.y - PlayerInfo.physHead.transform.position.y;
        headdist = Mathf.Clamp(headdist, 0, Mathf.Infinity);
        float legclamp = legLength - (headdist * 2.5f);

        float moveupamount = Mathf.Clamp01(PlayerInfo.hipspace.TransformVector(moveFlat).y);
        moveupamount *= moveupmult;

        float legadjust = (animModelHips.position.y - animModelFootSelection().position.y) / legLength;
        if (PlayerInfo.climbing && !inputJump)
            legadjust = 0.5f;

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

        if(headcolliding)
            legadjust = Mathf.Clamp(legadjust, 0, legclamp);

        float relativeheight = hipspace.position.y - PlayerInfo.footspace.position.y;
        currentratio = relativeheight;
        float heightratio = relativeheight / legadjust;
        heightratio = Mathf.Clamp(heightratio, 0, 2);

        float forceadjust = maxforcemult;
        if (LucidInputActionRefs.jump.ReadValue<float>() == 1 && relativeheight <= maxlegmult)
            forceadjust *= jumpforcemult;

        float currentY = rb.velocity.y;
        float targetY = Mathf.LerpUnclamped(forceadjust, 0, heightratio);
        float diff = targetY - currentY;

        Vector3 calc = pushdir * (-Physics.gravity.y + (diff * forcesmoothness));
        NrmSlide(calc * rb.mass, out Vector3 slide, out float nrm);
        slide = -slide;

        Vector3 dir = hipspace.TransformVector(moveFlat);
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
        if (!inputSprint)
            moveadjust *= sprintmult;

        float diffmag = 1 - Mathf.Clamp01(rb.velocity.magnitude / moveadjust);
        moveadjust *= 1 + (diffmag * moveburst);

        float jumpadjust = inputJump ? directionaljumpmult : 1;
        Vector3 movetarget = hipspace.TransformVector(moveFlat) * moveadjust * jumpadjust;
        Vector3 relativevel = hipspace.InverseTransformVector(rb.velocity);
        relativevel.y = 0;
        relativevel = hipspace.TransformVector(relativevel);
        Vector3 movediff = movetarget - relativevel;
        Vector3 surfaction = -movediff * rb.mass;
        slide += surfaction;

        nrm = Mathf.Clamp(nrm, 0, Mathf.Infinity);

        float fmax = Mathf.Pow(nrm, 0.5f) * rb.mass * friction;
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

    public void OnHeadCollisionStay(Collision c)
    {
        headcolliding = true;
    }

    public void OnHeadCollisionExit(Collision c)
    {
        headcolliding = false;
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
        Vector3 diffL = legspaceL.position - PlayerInfo.footTargetL;
        Vector3 diffR = legspaceR.position - PlayerInfo.footTargetR;
        Vector3 avg = (diffL + diffR) / 2;
        float strength = 1 - Mathf.Clamp01(avg.magnitude / legLength);
        avg = avg.normalized * strength;

        rb.AddForce(avg * slidepushforce, ForceMode.Acceleration);
    }

    //quickly splits a vector into its movement along and against the surface of footspace
    private void NrmSlide(Vector3 target, out Vector3 slide, out float nrm)
    {
        Vector3 relative = PlayerInfo.footspace.InverseTransformVector(target);
        slide = relative;
        slide.y = 0;
        slide = PlayerInfo.footspace.TransformVector(slide);
        nrm = relative.y;
    }

    //mostly handles the calculation of the virtual floor and leg animations
    private void PseudoWalk() 
    {
        Transform pelvis = PlayerInfo.pelvis;

        Vector2 moveVector = LucidInputActionRefs.movement.ReadValue<Vector2>();
        Vector3 moveFlat = Vector3.zero;
        moveFlat.x = moveVector.x;
        moveFlat.z = moveVector.y;

        bool currentright = animPhase < 0.5f;

        float stepphase = 0.5f - animPhase;
        stepphase = Mathf.Abs(stepphase) * 2;
        PlayerInfo.stepphase = stepphase;

        Vector3 willflat = (PlayerInfo.hipspace.TransformVector(moveFlat) * (movespeed / 2));
        willflat.y = 0;

        Vector3 velflat = rb.velocity;
        velflat.y = 0;

        Vector3 velrelative = PlayerInfo.pelvis.InverseTransformVector(velflat);

        Vector3 legR = legspaceR.position;
        legR += legspaceR.up * 0.1f;
        Vector3 legL = legspaceL.position;
        legL += legspaceL.up * 0.1f;

        float downadjust = -down;
        if (!PlayerInfo.grounded)
            downadjust = -down * airdownmult;
        downadjust += PlayerInfo.pelvis.position.y;

        Vector3 veladjust = velrelative;
        if (!PlayerInfo.grounded)
            veladjust = moveFlat * moveflatprobemult;

        Vector3 probeN = pelvis.position + (PlayerInfo.pelvis.forward * Mathf.Clamp(Mathf.Abs(veladjust.z) * probemult, probeZminimumOffset, Mathf.Infinity));
        probeN.y = (Vector3.up * downadjust).y;
        Vector3 probeS = pelvis.position - (PlayerInfo.pelvis.forward * Mathf.Clamp(Mathf.Abs(veladjust.z) * probemult, probeZminimumOffset, Mathf.Infinity));
        probeS.y = (Vector3.up * downadjust).y;
        Vector3 probeE = pelvis.position + (PlayerInfo.pelvis.right * Mathf.Clamp(Mathf.Abs(veladjust.x) * probemult, probeXminimumOffset, Mathf.Infinity));
        probeE.y = (Vector3.up * downadjust).y;
        Vector3 probeW = pelvis.position - (PlayerInfo.pelvis.right * Mathf.Clamp(Mathf.Abs(veladjust.x) * probemult, probeXminimumOffset, Mathf.Infinity));
        probeW.y = (Vector3.up * downadjust).y;

        //probe Z
        Ray N = new Ray(pelvis.position, probeN - pelvis.position);
        Ray S = new Ray(pelvis.position, probeS - pelvis.position);
        CastWalk(N, S, out RaycastHit hitN, out RaycastHit hitS, -1);

        //probe X
        Ray E = new Ray(legR, probeE - pelvis.position);
        Ray W = new Ray(legL, probeW - pelvis.position);
        CastWalk(E, W, out RaycastHit hitE, out RaycastHit hitW, -1);

        float diffZ = Mathf.Abs(hitN.point.y - hitS.point.y);
        float diffX = Mathf.Abs(hitW.point.y - hitE.point.y);

        List<RaycastHit> results = new List<RaycastHit>();
        if (diffZ < diffX)
        {
            if (hitN.point != Vector3.zero && hitN.distance < maxProbeOffset)
                results.Add(hitN);
            if (hitS.point != Vector3.zero && hitS.distance < maxProbeOffset)
                results.Add(hitS);
            if (hitE.point != Vector3.zero && hitE.distance < maxProbeOffset)
                results.Add(hitE);
            if (hitW.point != Vector3.zero && hitW.distance < maxProbeOffset)
                results.Add(hitW);
        }
        else
        {
            if (hitE.point != Vector3.zero && hitE.distance < maxProbeOffset)
                results.Add(hitE);
            if (hitW.point != Vector3.zero && hitW.distance < maxProbeOffset)
                results.Add(hitW);
            if (hitN.point != Vector3.zero && hitN.distance < maxProbeOffset)
                results.Add(hitN);
            if (hitS.point != Vector3.zero && hitS.distance < maxProbeOffset)
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
                Vector3 pos3 = PlayerInfo.pelvis.position;

                if (Vector3.Distance(pos3, pos2) <= Vector3.Distance(pos3, pos1))
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
                    c1 += PlayerInfo.pelvis.forward * 0.01f;
                }

                normal = Vector3.Cross(b1 - a1, c1 - a1);
                center = (a1 + b1 + c1) / 3;
                normal.Normalize();
                if (normal.y < 0)
                    normal = -normal;

                //Debug.DrawLine(a1, b1, Color.blue);
                //Debug.DrawLine(b1, c1, Color.blue);
                //Debug.DrawLine(c1, a1, Color.blue);

                break;

            case 2:
                Vector3 dir = PlayerInfo.hipspace.TransformVector(moveFlat);
                if(dir != Vector3.zero)
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
                center = PlayerInfo.pelvis.position + (Vector3.down * legLength);
                break;
        }
        if (true)
        {
            if(Vector3.Distance(pelvis.position, hitN.point) < maxProbeOffset)
                Debug.DrawLine(pelvis.position, hitN.point, Color.green);
            if (Vector3.Distance(pelvis.position, hitS.point) < maxProbeOffset)
                Debug.DrawLine(pelvis.position, hitS.point, Color.green);
            if (Vector3.Distance(legR, hitE.point) < maxProbeOffset)
                Debug.DrawLine(legR, hitE.point, Color.green);
            if (Vector3.Distance(legL, hitW.point) < maxProbeOffset)
                Debug.DrawLine(legL, hitW.point, Color.green);
            //Debug.DrawRay(center, normal, new Color(0, 255, 255));
        }

        Vector3 hfwd = Vector3.ProjectOnPlane(PlayerInfo.head.forward, normal);
        if (Vector3.Dot(PlayerInfo.pelvis.forward, normal) <= hipspaceMaxRot)
            hfwd += Vector3.up;
        Quaternion q1 = hipspace.rotation;
        Quaternion q2 = Quaternion.LookRotation(hfwd, normal);
        Quaternion q3 = Quaternion.RotateTowards(q1, q2, Quaternion.Angle(q1, q2) * (1 - hipspacesmoothness));
        hipspace.rotation = q3;

        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, hipspace.up);

        vfloor.position = center;
        vfloor.forward = -normal;

        float ratio = currentratio / ratiomult;
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
            if (animPhase > 1)
                animPhase -= 1;
        }
        PlayerInfo.animphase = animPhase;
    }

    private void CastWalk(Ray left, Ray right, out RaycastHit hitL, out RaycastHit hitR, float radius = -1)
    {
        if (radius == -1)
            radius = legWidth * legWidthMult;

        int hitLeft1 = Physics.SphereCastNonAlloc(left.origin, radius, left.direction, spherecastHitBufferL, 1000, Shortcuts.geometryMask);
        int hitRight1 = Physics.SphereCastNonAlloc(right.origin, radius, right.direction, spherecastHitBufferR, 1000, Shortcuts.geometryMask);
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

