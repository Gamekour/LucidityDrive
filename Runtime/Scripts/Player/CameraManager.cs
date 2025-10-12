using UnityEngine;
using UnityEngine.InputSystem;

namespace LucidityDrive
{
    public class CameraManager : MonoBehaviour
    {
        public static int cameraPointIndex = 0;

        [SerializeField] Transform[] cameraPoints;
        [SerializeField] Transform headRoot;
        [SerializeField] int defaultCameraPoint = 0;
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
            minAngle,
            FPCollisionRadius
            ;
        [SerializeField] LayerMask layerMaskNormal, layerMaskFP;

        private Transform headrootTarget;
        private bool forceMouselook = false;
        private bool FPCollision;
        private float currentMouselookBlend = 0;
        private float currentspeed;
        private Quaternion smoothDeriv = Quaternion.identity;
        private Vector3 externalDampRef = Vector3.zero;
        private Vector3 FPcollisionOffset;
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
            ChangeCam(defaultCameraPoint);
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
            LucidPlayerInfo.inFirstPerson = index == 0;
            if (LucidPlayerInfo.inFirstPerson)
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

        private void FixedUpdate()
        {
            if (!LucidPlayerInfo.animModelInitialized || LucidPlayerInfo.mainCamera == null) return;

            Vector3 FPCamPos = cameraPoints[0].position;

            bool cast1 = Physics.SphereCast(LucidPlayerInfo.pelvis.position, FPCollisionRadius, headrootTarget.position - LucidPlayerInfo.pelvis.position, out RaycastHit hitInfo, Vector3.Distance(LucidPlayerInfo.pelvis.position, headrootTarget.position) + 0.05f, LucidShortcuts.geometryMask);
                Debug.DrawLine(LucidPlayerInfo.pelvis.position, headrootTarget.position);
            bool cast2 = Physics.SphereCast(headrootTarget.position, FPCollisionRadius, FPCamPos - headrootTarget.position, out RaycastHit hitInfo2, Vector3.Distance(FPCamPos, headrootTarget.position) + 0.05f, LucidShortcuts.geometryMask);
                Debug.DrawLine(headrootTarget.position, FPCamPos);

            FPCollision = cast1 || cast2;
            if (FPCollision)
            {
                float hitDist = hitInfo.distance;
                if (!cast1)
                    hitDist = hitInfo2.distance;
                float totalDist = Vector3.Distance(LucidPlayerInfo.pelvis.position, headrootTarget.position);
                if (!cast1)
                    totalDist = Vector3.Distance(FPCamPos, headrootTarget.position);

                FPcollisionOffset = (LucidPlayerInfo.pelvis.position - headrootTarget.position).normalized * (Mathf.Abs(totalDist - hitDist) + FPCollisionRadius);
                if (!cast1)
                    FPcollisionOffset = (headrootTarget.position - FPCamPos).normalized * (Mathf.Abs(totalDist - hitDist) + FPCollisionRadius);
            }
        }

        private void LateUpdate()
        {
            if (!LucidPlayerInfo.animModelInitialized || LucidPlayerInfo.mainCamera == null) return;

            if (headrootTarget != null && headRoot != null)
            {
                if (cameraPointIndex != 0)
                    headRoot.position = Vector3.SmoothDamp(headRoot.position, headrootTarget.position, ref externalDampRef, nonFPCameraSmoothTime);
                else
                    headRoot.position = headrootTarget.position;


                Quaternion targetRotation = headrootTarget.rotation;
                Quaternion smoothRotation = SmoothDamp(headRoot.rotation, targetRotation, ref smoothDeriv, headRotationSmoothTime);

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
            if (cameraPointIndex == 0 && FPCollision)
                LucidPlayerInfo.mainCamera.transform.position += FPcollisionOffset;
            LucidPlayerInfo.mainCamera.transform.rotation = cameraPoints[cameraPointIndex].rotation;
        }

        public Quaternion SmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time)
        {
            if (Time.deltaTime < Mathf.Epsilon) return rot;

            var dot = Quaternion.Dot(rot, target);
            var mult = dot > 0f ? 1f : -1f;
            target.x *= mult;
            target.y *= mult;
            target.z *= mult;
            target.w *= mult;

            var result = new Vector4(
                Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
                Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
                Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
                Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
            ).normalized;

            var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), result);
            deriv.x -= derivError.x;
            deriv.y -= derivError.y;
            deriv.z -= derivError.z;
            deriv.w -= derivError.w;

            return new Quaternion(result.x, result.y, result.z, result.w);
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
}