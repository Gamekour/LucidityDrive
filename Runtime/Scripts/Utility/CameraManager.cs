using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] Transform[] camerapoints;
    [SerializeField] Transform headroot;
    [SerializeField] float headsmooth;
    [SerializeField] float headsmoothZ;
    [SerializeField] float rawblend = 0.5f;
    [SerializeField] LayerMask layermaskNormal;
    [SerializeField] LayerMask layermaskFP;
    private int cameraPointIndex = 0;
    private Transform headrootTarget;
    private bool forceRaw = false;

    private void Start()
    {
        LucidInputActionRefs.camselect1.started += CameraSwitch1;
        LucidInputActionRefs.camselect2.started += CameraSwitch2;
        LucidInputActionRefs.camselect3.started += CameraSwitch3;
        LucidInputActionRefs.camselect4.started += CameraSwitch4;
        PlayerInfo.OnAssignVismodel.AddListener(AssignVismodel);
        PlayerInfo.FPTransform = camerapoints[0];
    }

    private void OnDisable()
    {
        LucidInputActionRefs.camselect1.started -= CameraSwitch1;
        LucidInputActionRefs.camselect2.started -= CameraSwitch2;
        LucidInputActionRefs.camselect3.started -= CameraSwitch3;
        LucidInputActionRefs.camselect4.started -= CameraSwitch4;
        PlayerInfo.OnAssignVismodel.RemoveListener(AssignVismodel);
    }

    public void AssignVismodel(LucidVismodel vismodel)
    {
        headrootTarget = vismodel.anim.GetBoneTransform(HumanBodyBones.Head);
    }

    private void CameraSwitch1(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        ChangeCam(0);
    }
    private void CameraSwitch2(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        ChangeCam(1);
    }
    private void CameraSwitch3(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        ChangeCam(2);
    }
    private void CameraSwitch4(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        ChangeCam(3);
    }
    private void ChangeCam(int index)
    {
        cameraPointIndex = index;
        if (index == 0)
            PlayerInfo.mainCamera.cullingMask = layermaskFP;
        else
            PlayerInfo.mainCamera.cullingMask = layermaskNormal;
        if (index == 3)
        {
            Transform tcam = camerapoints[3].transform;
            tcam.position = PlayerInfo.mainCamera.transform.position;
            tcam.rotation = PlayerInfo.mainCamera.transform.rotation;
        }
    }

    private void LateUpdate()
    {
        if(headrootTarget != null && headroot != null) 
        {

            if (cameraPointIndex != 0)
                headroot.position = Vector3.Lerp(headroot.position, headrootTarget.position, 0.5f);
            else
                headroot.position = headrootTarget.position;
            Vector3 neweulers = Quaternion.Slerp(headroot.rotation, headrootTarget.rotation, 1 - headsmooth).eulerAngles;
            bool startpolar = headrootTarget.up.y > 0;
            bool endpolar = headroot.up.y > 0;
            if (startpolar == endpolar)
                neweulers.z = Mathf.LerpAngle(headroot.eulerAngles.z, headrootTarget.eulerAngles.z, 1 - headsmoothZ);
            else
                neweulers.z = headrootTarget.eulerAngles.z;

            float blendadjust = rawblend;
            if (forceRaw)
                blendadjust = 1;
            Quaternion raw = PlayerInfo.head.transform.rotation;
            Quaternion finalrot = Quaternion.Slerp(Quaternion.Euler(neweulers), raw, blendadjust);
            headroot.rotation = finalrot;
        }

        PlayerInfo.mainCamera.transform.position = camerapoints[cameraPointIndex].position;
        PlayerInfo.mainCamera.transform.rotation = camerapoints[cameraPointIndex].rotation;
    }

    public void OnHeadCollisionStay(Collision c)
    {
        forceRaw = true;
    }

    public void OnHeadCollisionExit(Collision c)
    {
        forceRaw = false;
    }
}
