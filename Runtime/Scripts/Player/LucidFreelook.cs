using System.Collections;
using UnityEngine;

public class LucidFreelook : MonoBehaviour
{
    [SerializeField] Vector2 sensitivity = Vector2.one;
    [SerializeField] float 
        headRadius,
        downAngleLimit,
        upAngleLimit,
        sensitivityMultiplier;

    private Transform chest;
    private Transform head;

    private float currentOffset, targetOffset;

    private void Awake()
    {
        LucidPlayerInfo.head = transform;
    }

    private void OnEnable()
    {
        LucidPlayerInfo.OnAssignVismodel.AddListener(AssignVismodel);
        LucidPlayerInfo.OnAnimModellInitialized.AddListener(OnAnimModelInitialized);
    }

    private void OnDisable()
    {
        LucidPlayerInfo.OnAssignVismodel.RemoveListener(AssignVismodel);
        LucidPlayerInfo.OnAnimModellInitialized.RemoveListener(OnAnimModelInitialized);
    }

    private void Update()
    {
        if (head == null) return;
        transform.position = head.position;
        if (LucidPlayerInfo.mainCamera == null || chest == null || !LucidPlayerInfo.headLocked || !LucidPlayerInfo.animModelInitialized)
            return;

        CalculateRotation();
    }

    private void OnAnimModelInitialized()
    {
        head = LucidPlayerInfo.animationModel.GetBoneTransform(HumanBodyBones.Head);
    }

    public void AssignVismodel(LucidVismodel vismodel)
    {
        chest = vismodel.anim.GetBoneTransform(HumanBodyBones.Chest);
    }

    private void CalculateRotation()
    {
        LucidPlayerInfo.mainBody.inertiaTensorRotation = Quaternion.identity;

        Vector2 headLookInput = LucidInputValueShortcuts.headLook;
        headLookInput *= sensitivity * sensitivityMultiplier;
        headLookInput *= Time.deltaTime;

        Vector3 headForwardFlat = transform.forward;
        headForwardFlat.y = 0;
        Vector3 hipForwardFlat = LucidPlayerInfo.pelvis.forward;
        hipForwardFlat.y = 0;

        Vector3 rotation = new(headLookInput.y, headLookInput.x, 0);

        float chestX = chest.eulerAngles.x;

        if (chest.up.y < 0)
            chestX = -chestX;

        float currentX = transform.eulerAngles.x;

        if (transform.up.y < 0)
            currentX = AdjustForInvertedState(currentX, 90f, 270f);

        currentOffset = CalculateOffset(chestX, currentX);
        if (chest.up.y > 0)
            targetOffset = Mathf.Clamp(currentOffset + rotation.x, downAngleLimit, upAngleLimit);
        else
        {
            if (currentOffset + rotation.x > 0)
                targetOffset = Mathf.Clamp(currentOffset + rotation.x, upAngleLimit, 360);
            else if (currentOffset + rotation.x < upAngleLimit)
                targetOffset = Mathf.Clamp(currentOffset + rotation.x, -360, downAngleLimit);
            else
                targetOffset = currentOffset + rotation.x;
        }

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

    public void SetSensitivity(float sens)
    {
        sensitivityMultiplier = sens;
    }
}
