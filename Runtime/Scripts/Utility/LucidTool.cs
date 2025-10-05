using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LucidTool : MonoBehaviour
{
    public Transform itemPosesR;
    public Transform itemPosesL;
    public Transform PrimaryGripL;
    public Transform SecondaryGripL;
    public Transform PrimaryGripR;
    public Transform SecondaryGripR;
    public Transform ItemPosePrimaryL;
    public Transform ItemPoseSecondaryL;
    public Transform ItemPosePrimaryR;
    public Transform ItemPoseSecondaryR;
    public bool GrabLockPrimary = true;
    public bool GrabLockSecondary = false;
    public bool autoGrabL = false;
    public bool autoGrabR = false;
    public bool switchGrabOrder = false;
    public bool disableDrop = false;

    [HideInInspector]
    public bool held = false;

    public UnityEvent OnUse;
    public UnityEvent OnUseUp;
    public UnityEvent OnGrab;
    public UnityEvent OnDrop;

    public void OnEnable()
    {
        StartCoroutine(WaitForInit());
    }

    public void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator WaitForInit()
    {
        LucidArms la = FindObjectOfType<LucidArms>();
        while (!LucidPlayerInfo.animModelInitialized)
            yield return null;

        if (autoGrabL || autoGrabR)
        {
            la.ForceUngrab(true);
            la.ForceUngrab(false);
        }
        if (!switchGrabOrder)
        {
            if (autoGrabR)
            {
                transform.position = LucidPlayerInfo.pelvis.position;
                la.ForceGrab(this, true);
            }
            if (autoGrabL)
            {
                transform.position = LucidPlayerInfo.pelvis.position;
                la.ForceGrab(this, false);
            }
        }
        else
        {
            if (autoGrabL)
            {
                transform.position = LucidPlayerInfo.pelvis.position;
                la.ForceGrab(this, false);
            }
            if (autoGrabR)
            {
                transform.position = LucidPlayerInfo.pelvis.position;
                la.ForceGrab(this, true);
            }
        }
    }
}
