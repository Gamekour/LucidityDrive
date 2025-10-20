using UnityEngine;

namespace LucidityDrive
{
    public class Freelook : MonoBehaviour
    {
        [Tooltip("Look-sensitivity")]
        public float sensitivity = 9;
        [Tooltip("Scale sensitivity for X and Y separately")]
        public Vector2 sensitivityModifier = Vector2.one;
        [Tooltip("Clamp the percieved delta-time to this value (reduces camera movement during frame stutters)")]
        [SerializeField] float maxDeltaTime = 0.06f;

        private Transform chest;
        private Transform head;

        private float currentOffset, targetOffset;

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
            if (head == null) return;
            transform.position = head.position;
            if (Camera.main == null || chest == null || !PlayerInfo.headLocked || !PlayerInfo.animModelInitialized)
                return;

            CalculateRotation();
        }

        private void OnAnimModelInitialized()
        {
            head = PlayerInfo.animationModel.GetBoneTransform(HumanBodyBones.Head);
        }

        public void AssignVismodel(Vismodel vismodel)
        {
            chest = vismodel.anim.GetBoneTransform(HumanBodyBones.Chest);
        }

        private void CalculateRotation()
        {
            PlayerInfo.mainBody.inertiaTensorRotation = Quaternion.identity;

            Vector2 headLookInput = LucidInputValueShortcuts.headLook;
            headLookInput *= sensitivityModifier * sensitivity;
            headLookInput *= Mathf.Clamp(Time.deltaTime, 0, maxDeltaTime);

            Vector3 headForwardFlat = transform.forward;
            headForwardFlat.y = 0;
            Vector3 hipForwardFlat = PlayerInfo.pelvis.forward;
            hipForwardFlat.y = 0;

            Vector3 rotation = new(headLookInput.y, headLookInput.x, 0);

            float chestX = chest.eulerAngles.x;

            if (chest.up.y < 0)
                chestX = -chestX;

            float currentX = transform.eulerAngles.x;

            if (transform.up.y < 0)
                currentX = AdjustForInvertedState(currentX, 90f, 270f);

            currentOffset = CalculateOffset(chestX, currentX);
            targetOffset = currentOffset + rotation.x;

            float offsetDifference = targetOffset - currentOffset;

            RotateModel(rotation.y, offsetDifference);
        }

        private float AdjustForInvertedState(float angle, float threshold1, float threshold2)
        {
            float extra = (angle > 180f) ? angle - threshold2 : angle - threshold1;
            return angle - 2 * extra;
        }

        private float CalculateOffset(float chestX, float currentX)
        {
            float offset = chestX - currentX;

            if (Mathf.Abs(offset) > 180f)
            {
                if (chestX > 180f)
                {
                    offset = chestX - (currentX + 360f);
                }
                else
                {
                    offset = chestX - (currentX - 360f);
                }
            }

            return offset;
        }

        private void RotateModel(float yRotation, float xRotationDifference)
        {
            transform.Rotate(Vector3.up, yRotation, Space.World);
            transform.Rotate(Vector3.right, -xRotationDifference, Space.Self);

            Vector3 localEulerAngles = transform.localEulerAngles;
            localEulerAngles.x = 0;
        }
    }
}