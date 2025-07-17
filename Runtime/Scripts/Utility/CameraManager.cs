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
        LucidPlayerInfo.mainCamera = Camera.main;
        LucidInputActionRefs.camSelect1.started += CameraSwitch1;
        LucidInputActionRefs.camSelect2.started += CameraSwitch2;
        LucidInputActionRefs.camSelect3.started += CameraSwitch3;
        LucidInputActionRefs.camSelect4.started += CameraSwitch4;
        LucidInputActionRefs.camCycle.started += CameraCycle;
        LucidPlayerInfo.OnAssignVismodel.AddListener(AssignVismodel);
        LucidPlayerInfo.FPTransform = cameraPoints[0];
    }

    private void OnDisable()
    {
        LucidInputActionRefs.camSelect1.started -= CameraSwitch1;
        LucidInputActionRefs.camSelect2.started -= CameraSwitch2;
        LucidInputActionRefs.camSelect3.started -= CameraSwitch3;
        LucidInputActionRefs.camSelect4.started -= CameraSwitch4;
        LucidInputActionRefs.camCycle.started -= CameraCycle;
        LucidPlayerInfo.OnAssignVismodel.RemoveListener(AssignVismodel);
    }

    public void AssignVismodel(LucidVismodel vismodel)
    {
        headrootTarget = vismodel.anim.GetBoneTransform(HumanBodyBones.Head);
        LucidPlayerInfo.mainCamera.cullingMask = layerMaskFP;
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
        if (!LucidPlayerInfo.headLocked) return;
        
        cameraPointIndex = index;
        if (index == 0)
            LucidPlayerInfo.mainCamera.cullingMask = layerMaskFP;
        else
            LucidPlayerInfo.mainCamera.cullingMask = layerMaskNormal;
        if (index == 3)
        {
            Transform tcam = cameraPoints[3].transform;
            tcam.position = LucidPlayerInfo.mainCamera.transform.position;
            tcam.rotation = LucidPlayerInfo.mainCamera.transform.rotation;
        }
    }

    private void Update()
    {
        float speed = LucidPlayerInfo.mainBody.velocity.magnitude;
        if (speed < fovMinSpeed)
            speed = 0;
        currentspeed = Mathf.SmoothDamp(currentspeed, speed, ref fovDampRef, fovDampTime);
        LucidPlayerInfo.mainCamera.fieldOfView = Mathf.Clamp(fovBase + (currentspeed * fovBySpeed), fovBase, fovMax);
    }

    private void LateUpdate()
    {
        if (!LucidPlayerInfo.animModelInitialized || LucidPlayerInfo.mainCamera == null) return;

        if (headrootTarget != null && headRoot != null)
        {
            if (cameraPointIndex != 0)
                headRoot.position = Vector3.SmoothDamp(headRoot.position, headrootTarget.position, ref externalDampRef, nonFPCameraSmoothTime);
            else
            {
                bool hit = Physics.SphereCast(LucidPlayerInfo.pelvis.position, 0.1f, headrootTarget.position - LucidPlayerInfo.pelvis.position, out RaycastHit hitInfo, Vector3.Distance(LucidPlayerInfo.pelvis.position, headrootTarget.position) + 0.05f, LucidShortcuts.geometryMask);
                if (hit)
                {
                    float hitDist = hitInfo.distance;
                    float totalDist = Vector3.Distance(LucidPlayerInfo.pelvis.position, headrootTarget.position);

                    Vector3 offset = (LucidPlayerInfo.pelvis.position - headrootTarget.position).normalized * (Mathf.Abs(totalDist - hitDist) + 0.05f);
                    headRoot.position = headrootTarget.position + offset;
                }
                else
                    headRoot.position = headrootTarget.position;
            }

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
            Quaternion raw = LucidPlayerInfo.head.transform.rotation;
            Quaternion finalRotation = Quaternion.Slerp(smoothRotation, raw, currentMouselookBlend);
            float totalAngle = Quaternion.Angle(finalRotation, raw);
            if (totalAngle > minAngle)
                headRoot.rotation = finalRotation;
            else
                headRoot.rotation = raw;
        }

        // Update camera position and rotation
        LucidPlayerInfo.mainCamera.transform.position = cameraPoints[cameraPointIndex].position;
        LucidPlayerInfo.mainCamera.transform.rotation = cameraPoints[cameraPointIndex].rotation;
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
