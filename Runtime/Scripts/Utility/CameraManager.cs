using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    public static int cameraPointIndex = 0;

    [SerializeField] Transform[] cameraPoints;
    [SerializeField] Transform headRoot;
    [SerializeField]
    float
        headRotationSmoothTime,
        mouselookBlend,
        mouselookBlendTransitionSpeed,
        nonFPCameraSmoothTime,
        fovBase,
        fovBySpeed,
        fovDampTime,
        fovMinSpeed,
        fovMax,
        minAngle;
    [SerializeField] LayerMask layerMaskNormal, layerMaskFP;

    private Transform headrootTarget;
    private bool forceMouselook = false;
    private float currentMouselookBlend = 0;
    private float currentspeed;
    private Quaternion smoothDeriv = Quaternion.identity;
    private Vector3 externalDampRef = Vector3.zero;
    private float fovDampRef;

    private void Start()
    {
        PlayerInfo.mainCamera = Camera.main;
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
        PlayerInfo.mainCamera.cullingMask = layerMaskFP;
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
            PlayerInfo.mainCamera.cullingMask = layerMaskFP;
        else
            PlayerInfo.mainCamera.cullingMask = layerMaskNormal;
        if (index == 3)
        {
            Transform tcam = cameraPoints[3].transform;
            tcam.position = PlayerInfo.mainCamera.transform.position;
            tcam.rotation = PlayerInfo.mainCamera.transform.rotation;
        }
    }

    private void Update()
    {
        float speed = PlayerInfo.mainBody.velocity.magnitude;
        if (speed < fovMinSpeed)
            speed = 0;
        currentspeed = Mathf.SmoothDamp(currentspeed, speed, ref fovDampRef, fovDampTime);
        PlayerInfo.mainCamera.fieldOfView = Mathf.Clamp(fovBase + (currentspeed * fovBySpeed), fovBase, fovMax);
    }

    private void LateUpdate()
    {
        if (!PlayerInfo.animModelInitialized || PlayerInfo.mainCamera == null) return;

        if (headrootTarget != null && headRoot != null)
        {
            if (cameraPointIndex != 0)
                headRoot.position = Vector3.SmoothDamp(headRoot.position, headrootTarget.position, ref externalDampRef, nonFPCameraSmoothTime);
            else
                headRoot.position = headrootTarget.position;
            
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
            float totalAngle = Quaternion.Angle(finalRotation, raw);
            if (totalAngle > minAngle)
                headRoot.rotation = finalRotation;
            else
                headRoot.rotation = raw;
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
