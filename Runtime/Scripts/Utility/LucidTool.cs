using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LucidTool : MonoBehaviour
{
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
    public bool disableDrop = false;

    public UnityEvent OnUse;
    public UnityEvent OnUseUp;
    public UnityEvent OnGrab;
    public UnityEvent OnDrop;

    public void OnEnable()
    {
        LucidPlayerInfo.OnAssignVismodel.AddListener(OnVismodelAssigned);
    }

    public void OnDisable()
    {
        LucidPlayerInfo.OnAssignVismodel.RemoveListener(OnVismodelAssigned);
    }

    private void OnVismodelAssigned(LucidVismodel v)
    {
        StartCoroutine(WaitForInit());
    }

    IEnumerator WaitForInit()
    {
        yield return new WaitForFixedUpdate();
        if (autoGrabL)
            FindObjectOfType<LucidArms>().ForceGrab(this, false);
        else if (autoGrabR)
            FindObjectOfType<LucidArms>().ForceGrab(this, true);
    }
}
