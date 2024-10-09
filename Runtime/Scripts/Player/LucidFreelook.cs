using System.Collections;
using Unity.VisualScripting.YamlDotNet.Serialization.NodeTypeResolvers;
using UnityEngine;

public class LucidFreelook : MonoBehaviour
{
    [SerializeField] private Vector2 sensitivity = Vector2.one;
    [SerializeField] private float headRadius;
    [SerializeField] private float downAngleLimit;
    [SerializeField] private float upAngleLimit;
    [SerializeField] private float sensitivityMultiplier = 1f;

    private Transform chest;
    private Transform head;

    private float chestXAngle;
    private float adjustedUpAngle;
    private float adjustedDownAngle;
    private float realAdjustedDownAngle;
    private float realAdjustedUpAngle;
    private float currentXAngle;
    private float currentOffset;
    private float targetOffset;

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
        if (PlayerInfo.mainCamera == null || chest == null || !PlayerInfo.headlocked || !PlayerInfo.animModelInitialized)
            return;

        transform.position = head.position;
        CalculateRotation();
    }

    private void OnAnimModelInitialized()
    {
        head = PlayerInfo.playermodelAnim.GetBoneTransform(HumanBodyBones.Head);
    }

    public void AssignVismodel(LucidVismodel vismodel)
    {
        chest = vismodel.anim.GetBoneTransform(HumanBodyBones.Chest);
    }

    public void SetSensitivityMultiplier(float newValue)
    {
        sensitivityMultiplier = newValue;
    }

    private void CalculateRotation()
    {
        PlayerInfo.mainBody.inertiaTensorRotation = Quaternion.identity;

        Vector2 headLookInput = LucidInputActionRefs.headlook.ReadValue<Vector2>();
        headLookInput *= sensitivity * sensitivityMultiplier;
        headLookInput *= Time.deltaTime;

        Vector3 headForwardFlat = transform.forward;
        headForwardFlat.y = 0;
        Vector3 hipForwardFlat = PlayerInfo.pelvis.forward;
        hipForwardFlat.y = 0;

        float angleDifference = Mathf.Clamp(Vector3.SignedAngle(headForwardFlat, hipForwardFlat, Vector3.up), -90f, 90f);
        bool isLookingLeft = (angleDifference > 0);
        float yFactor = 1f - (Mathf.Abs(angleDifference) / 90f);

        Vector3 rotation = new Vector3(headLookInput.y, headLookInput.x, 0);

        if ((rotation.y < 0 && isLookingLeft) || (rotation.y > 0 && !isLookingLeft))
        {
            // Uncomment the following line if needed:
            // rotation.y *= yFactor;
        }

        float chestX = chest.eulerAngles.x;
        chestXAngle = chestX;

        float downAdjust = chestX + downAngleLimit;
        realAdjustedDownAngle = downAdjust;
        float upAdjust = chestX - upAngleLimit;
        realAdjustedUpAngle = upAdjust;

        if (chestX > 180f)
        {
            if (downAdjust > 360f)
            {
                realAdjustedDownAngle = downAdjust - 360f;
            }
        }

        if (chestX < 180f && upAdjust < 0f)
        {
            realAdjustedUpAngle = upAdjust + 360f;
        }

        AdjustChestXForUpsideDown();

        float currentX = transform.eulerAngles.x;

        AdjustForUpsideDown(ref currentX);

        adjustedUpAngle = upAdjust;
        adjustedDownAngle = downAdjust;
        currentXAngle = currentX;

        currentOffset = CalculateOffset(chestX, currentX);
        targetOffset = Mathf.Clamp(currentOffset + rotation.x, downAngleLimit, upAngleLimit);

        float offsetDifference = targetOffset - currentOffset;

        RotateModel(rotation.y, offsetDifference);
    }

    private void AdjustChestXForUpsideDown()
    {
        if (chest.up.y < 0)
        {
            if (chestXAngle > 180f)
            {
                float extra = chestXAngle - 270f;
                chestXAngle -= 2 * extra;
            }
            else if (chestXAngle < 180f)
            {
                float extra = chestXAngle - 90f;
                chestXAngle -= 2 * extra;
            }
        }
    }

    private void AdjustForUpsideDown(ref float currentX)
    {
        if (transform.up.y < 0)
        {
            if (currentX > 180f)
            {
                float extra = currentX - 270f;
                currentX -= 2 * extra;
            }
            else if (currentX < 180f)
            {
                float extra = currentX - 90f;
                currentX -= 2 * extra;
            }
        }
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
