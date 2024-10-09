using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LucidFreelook : MonoBehaviour
{
    [SerializeField] Vector2 sens;
    [SerializeField] float headradius;
    [SerializeField] float downfloat;
    [SerializeField] float upfloat;
    [SerializeField] float sensmult = 1;
    private Transform chest;
    private Transform animhead;
    private float chestXDisplay;
    private float adjustUp;
    private float adjustDown;
    private float adjustDownReal;
    private float adjustUpReal;
    private float chex;
    private float currentOffset;
    private float targetOffset;

    private void Awake()
    {
        PlayerInfo.head = transform;
    }

    private void OnEnable()
    {
        PlayerInfo.OnAssignVismodel.AddListener(AssignVismodel);
        PlayerInfo.OnAnimModellInitialized.AddListener(OnAnimModelInitialized);
    }

    private void OnDisable()
    {
        PlayerInfo.OnAssignVismodel.RemoveListener(AssignVismodel);
        PlayerInfo.OnAnimModellInitialized.RemoveListener(OnAnimModelInitialized);
    }

    private void Update()
    {
        if (PlayerInfo.mainCamera == null || chest == null || !PlayerInfo.headlocked || !PlayerInfo.animModelInitialized) return;

        transform.position = animhead.position;
        RotationCalc();
    }

    private void OnAnimModelInitialized()
    {
        animhead = PlayerInfo.playermodelAnim.GetBoneTransform(HumanBodyBones.Head);
    }

    public void AssignVismodel(LucidVismodel vismodel)
    {
        chest = vismodel.anim.GetBoneTransform(HumanBodyBones.Chest);
    }

    public void SetSensMult(float newValue)
    {
        sensmult = newValue;
    }

    private void RotationCalc()
    {
        PlayerInfo.mainBody.inertiaTensorRotation = Quaternion.identity;
        Vector2 headlook = LucidInputActionRefs.headlook.ReadValue<Vector2>();
        headlook *= sens * sensmult;
        headlook *= Time.deltaTime;

        Vector3 headflat = transform.forward;
        headflat.y = 0;
        Vector3 hipflat = PlayerInfo.pelvis.forward;
        hipflat.y = 0;

        float angle = Mathf.Clamp(Vector3.SignedAngle(headflat, hipflat, Vector3.up), -90, 90);
        bool left = (angle > 0);
        float y = 1 - (Mathf.Abs(angle) / 90);

        Vector3 rot = Vector3.zero;
        rot.x = headlook.y;
        rot.y = headlook.x;
        if ((rot.y < 0 && left) || (rot.y > 0 && !left))
        {
            //rot.y *= y;
        }

        float chestX = chest.eulerAngles.x;

        chestXDisplay = chestX;

        float downadjust = chestX + downfloat;
        float downadjustreal = downadjust;
        float upadjust = chestX - upfloat;
        float upadjustreal = upadjust;

        if(chestX > 180)
        {
            if (downadjust > 360)
            {
                downadjustreal = downadjust - 360;
            }
        }
        if (chestX < 180 && upadjust < 0)
            upadjustreal = upadjust + 360;
        if(chest.up.y < 0)
        {
            if(chestX > 180)
            {
                float extra = chestX - 270;
                chestX -= (2 * extra);
            }
            if(chestX < 180)
            {
                float extra = chestX - 90;
                chestX -= (2 * extra);
            }
        }

        float theX = transform.eulerAngles.x;

        if(transform.up.y < 0)
        {
            if(theX > 180)
            {
                float extra = theX - 270;
                theX -= (2 * extra);
            }
            if(theX < 180)
            {
                float extra = theX - 90;
                theX -= (2 * extra);
            }
        }

        adjustUp = upadjust;
        adjustDown = downadjust;
        adjustDownReal = downadjustreal;
        adjustUpReal = upadjustreal;
        chex = theX;

        float currentoffset = chestX - theX;
        if (Mathf.Abs(currentoffset) > 180)
        {
            if (chestX > 180)
                currentoffset = chestX - (theX + 360);
            else
                currentoffset = chestX - (theX - 360);
        }
        float targetoffset = Mathf.Clamp(currentoffset + rot.x, downfloat, upfloat);

        targetOffset = targetoffset;
        currentOffset = currentoffset;

        float diff = (targetoffset - currentoffset);

        transform.Rotate(Vector3.up, rot.y, Space.World);
        transform.Rotate(Vector3.right, -diff, Space.Self);
        Vector3 localeulers = transform.localEulerAngles;
        localeulers.x = 0;
    }
}
