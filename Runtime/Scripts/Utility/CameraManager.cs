using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    [SerializeField] Transform[] cameraPoints;
    [SerializeField] Transform headRoot;
    [SerializeField] float 
        headRotationSmoothTime,
        mouselookBlend,
        mouselookBlendTransitionSpeed,
        nonFPCameraSmoothTime;
    [SerializeField] LayerMask layermaskNormal, layermaskFP;

    private int cameraPointIndex = 0;
    private Transform headrootTarget;
    private bool forceMouselook = false;
    private float currentMouselookBlend = 0;
    private Quaternion smoothDeriv = Quaternion.identity;
    private Vector3 externalDampRef = Vector3.zero;

    private void Start()
    {
        LucidInputActionRefs.camSelect1.started += CameraSwitch1;
        LucidInputActionRefs.camSelect2.started += CameraSwitch2;
        LucidInputActionRefs.camSelect3.started += CameraSwitch3;
        LucidInputActionRefs.camSelect4.started += CameraSwitch4;
        LucidInputActionRefs.camCycle.started += CameraCycle;
        PlayerInfo.OnAssignVismodel.AddListener(AssignVismodel);
        PlayerInfo.FPTransform = cameraPoints[0];
    }

    private void OnDisable()
    {
        LucidInputActionRefs.camSelect1.started -= CameraSwitch1;
        LucidInputActionRefs.camSelect2.started -= CameraSwitch2;
        LucidInputActionRefs.camSelect3.started -= CameraSwitch3;
        LucidInputActionRefs.camSelect4.started -= CameraSwitch4;
        LucidInputActionRefs.camCycle.started -= CameraCycle;
        PlayerInfo.OnAssignVismodel.RemoveListener(AssignVismodel);
    }

    public void AssignVismodel(LucidVismodel vismodel)
    {
        headrootTarget = vismodel.anim.GetBoneTransform(HumanBodyBones.Head);
    }

    private void CameraSwitch1(InputAction.CallbackContext obj)
    {
        ChangeCam(0);
    }
    private void CameraSwitch2(InputAction.CallbackContext obj)
    {
        ChangeCam(1);
    }
    private void CameraSwitch3(InputAction.CallbackContext obj)
    {
        ChangeCam(2);
    }
    private void CameraSwitch4(InputAction.CallbackContext obj)
    {
        ChangeCam(3);
    }
    private void CameraCycle(InputAction.CallbackContext obj)
    {
        cameraPointIndex = (cameraPointIndex + 1) % 4;
        ChangeCam(cameraPointIndex);
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
            Transform tcam = cameraPoints[3].transform;
            tcam.position = PlayerInfo.mainCamera.transform.position;
            tcam.rotation = PlayerInfo.mainCamera.transform.rotation;
        }
    }

    private void LateUpdate()
    {
        if (!PlayerInfo.animModelInitialized) return;

        if (headrootTarget != null && headRoot != null)
        {
            // Handle head position interpolation
            if (cameraPointIndex != 0)
                headRoot.position = Vector3.SmoothDamp(headRoot.position, headrootTarget.position, ref externalDampRef, nonFPCameraSmoothTime);
            else
                headRoot.position = headrootTarget.position;

            // Handle rotation with quaternions to prevent gimbal lock
            Quaternion targetRotation = headrootTarget.rotation;
            Quaternion smoothRotation = QuaternionUtil.SmoothDamp(headRoot.rotation, targetRotation, ref smoothDeriv, headRotationSmoothTime);

            if (forceMouselook)
                currentMouselookBlend = 0;
            else
            {
                if (currentMouselookBlend < mouselookBlend)
                    currentMouselookBlend = Mathf.Clamp(currentMouselookBlend + (Time.deltaTime * mouselookBlendTransitionSpeed), 0, mouselookBlend);
            }

            // Combine raw rotation with smoothed rotation
            Quaternion raw = PlayerInfo.head.transform.rotation;
            Quaternion finalRotation = Quaternion.Slerp(smoothRotation, raw, currentMouselookBlend);
            headRoot.rotation = finalRotation;
        }

        // Update camera position and rotation
        PlayerInfo.mainCamera.transform.position = cameraPoints[cameraPointIndex].position;
        PlayerInfo.mainCamera.transform.rotation = cameraPoints[cameraPointIndex].rotation;
    }

    public void OnHeadCollisionStay(Collision c)
    {
        forceMouselook = true;
    }

    public void OnHeadCollisionExit(Collision c)
    {
        forceMouselook = false;
    }
}
