using UnityEngine;
using UnityEngine.InputSystem;

namespace LucidityDrive
{
    public class CameraManager : MonoBehaviour
    {
        public static int cameraPointIndex = 0;
        [HideInInspector] public float fovBase = 80;

        [Tooltip("Transforms for the camera to target in different camera modes")]
        [SerializeField] Transform[] cameraPoints;
        [Tooltip("Transform to attach to the player's head")]
        [SerializeField] Transform headRoot;
        [Tooltip("Default camera type (0 = First Person, 1 = Second Person, 2 = Third Person, 3 = Stationary)")]
        [SerializeField] int defaultCameraPoint = 0;
        [Tooltip("Smooth the camera rotation by this value")]
        [SerializeField] float headRotationSmoothTime = 0;
        [Tooltip("Mix between mouse movement and animated head movement; 0 = completely stable, 1= chaotic")]
        [SerializeField] float mouselookBlend = 0.1f;
        [Tooltip("Smooth the camera position by this value when not in first person")]
        [SerializeField] float nonFPCameraSmoothTime = 0.05f;
        [Tooltip("Change Field of View by this number, scaled by velocity")]
        [SerializeField] float fovBySpeed = 5;
        [Tooltip("Smooth time for Field of View change")]
        [SerializeField] float fovSmoothTime = 0.5f;
        [Tooltip("Minimum speed at which Field of View begins to change")]
        [SerializeField] float fovMinSpeed = 5;
        [Tooltip("Maximum Field of View")]
        [SerializeField] float fovMax = 120;
        [Tooltip("Radius of first-person camera collision check")]
        [SerializeField] float FPCollisionRadius = 0.03f;

        private Transform headrootTarget;
        private bool FPCollision;
        private float currentspeed;
        private Quaternion smoothDeriv = Quaternion.identity;
        private Vector3 externalDampRef = Vector3.zero;
        private Vector3 FPcollisionOffset;
        private float fovDampRef;

        private void Start()
        {
            LucidInputActionRefs.camSelect1.started += CameraSwitch1;
            LucidInputActionRefs.camSelect2.started += CameraSwitch2;
            LucidInputActionRefs.camSelect3.started += CameraSwitch3;
            LucidInputActionRefs.camSelect4.started += CameraSwitch4;
            LucidInputActionRefs.camCycle.started += CameraCycle;
            LucidPlayerInfo.OnAssignVismodel.AddListener(AssignVismodel);
            LucidPlayerInfo.FPTransform = cameraPoints[0];
            fovBase = Camera.main.fieldOfView;
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

        public void AssignVismodel(Vismodel vismodel)
        {
            headrootTarget = vismodel.anim.GetBoneTransform(HumanBodyBones.Head);
            ChangeCam(defaultCameraPoint, true);
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
        private void ChangeCam(int index, bool ignoreHeadlocked = false)
        {
            if (!LucidPlayerInfo.headLocked && !ignoreHeadlocked) return;

            cameraPointIndex = index;
            LucidPlayerInfo.inFirstPerson = index == 0;
            if (index == 3)
            {
                Transform tcam = cameraPoints[3].transform;
                tcam.position = Camera.main.transform.position;
                tcam.rotation = Camera.main.transform.rotation;
            }

            LucidPlayerInfo.onChangeCameraPoint.Invoke(cameraPointIndex);
        }

        private void Update()
        {
            float speed = LucidPlayerInfo.mainBody.velocity.magnitude;
            if (speed < fovMinSpeed)
                speed = 0;
            currentspeed = Mathf.SmoothDamp(currentspeed, speed, ref fovDampRef, fovSmoothTime);
            Camera.main.fieldOfView = Mathf.Clamp(fovBase + (currentspeed * fovBySpeed), fovBase, fovMax);
        }

        private void FixedUpdate()
        {
            if (!LucidPlayerInfo.animModelInitialized || Camera.main == null) return;

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
            if (!LucidPlayerInfo.animModelInitialized || Camera.main == null) return;

            if (headrootTarget != null && headRoot != null)
            {
                if (cameraPointIndex != 0)
                    headRoot.position = Vector3.SmoothDamp(headRoot.position, headrootTarget.position, ref externalDampRef, nonFPCameraSmoothTime);
                else
                    headRoot.position = headrootTarget.position;

                Quaternion targetRotation = headrootTarget.rotation;
                Quaternion smoothRotation = SmoothDamp(headRoot.rotation, targetRotation, ref smoothDeriv, headRotationSmoothTime);

                // Combine raw rotation with smoothed rotation
                Quaternion raw = LucidPlayerInfo.head.transform.rotation;
                Quaternion finalRotation = Quaternion.Slerp(smoothRotation, raw, mouselookBlend);
                float totalAngle = Quaternion.Angle(finalRotation, raw);
                if (totalAngle > 0)
                    headRoot.rotation = finalRotation;
                else
                    headRoot.rotation = raw;
            }

            // Update camera position and rotation
            Camera.main.transform.position = cameraPoints[cameraPointIndex].position;
            if (cameraPointIndex == 0 && FPCollision)
                Camera.main.transform.position += FPcollisionOffset;
            Camera.main.transform.rotation = cameraPoints[cameraPointIndex].rotation;
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
    }
}