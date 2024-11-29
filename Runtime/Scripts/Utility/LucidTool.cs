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

    public UnityEvent Use;
}
