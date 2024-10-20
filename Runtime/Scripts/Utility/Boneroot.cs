using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BoneRoot : MonoBehaviour
{
    [SerializeField] bool usePlayermodel = false;
    [SerializeField] HumanBodyBones targetBone;

    [Header("Force Match")]
    [SerializeField] bool useForceMatch;
    [SerializeField] float positionforce = 10;
    [SerializeField] float torque = 10;
    [SerializeField] float tolerance = 0.01f;
    private Transform targetTransform;
    private ConfigurableJoint joint;
    private Rigidbody rb;

    private ParentConstraint parentConstraint;
    private bool initialized = false;

    private void OnEnable()
    {
        if (!useForceMatch)
            parentConstraint = GetComponent<ParentConstraint>();
        else
        {
            joint = GetComponent<ConfigurableJoint>();
            rb = GetComponent<Rigidbody>();
        }
        PlayerInfo.OnAssignVismodel.AddListener(OnAssignVismodel);
        PlayerInfo.OnAnimModellInitialized.AddListener(OnInitializeAnimModel);
    }

    private void OnDisable()
    {
        PlayerInfo.OnAssignVismodel.RemoveListener(OnAssignVismodel);
        PlayerInfo.OnAnimModellInitialized.RemoveListener(OnInitializeAnimModel);
    }

    public void OnAssignVismodel(LucidVismodel newModel)
    {
        if (usePlayermodel) return;

        if (!useForceMatch)
            HardAttach(newModel.anim);
        else
            targetTransform = newModel.anim.GetBoneTransform(targetBone);
    }

    public void OnInitializeAnimModel()
    {
        if(!usePlayermodel) return;

        if (!useForceMatch)
            HardAttach(PlayerInfo.playermodelAnim);
        else
            targetTransform = PlayerInfo.playermodelAnim.GetBoneTransform(targetBone);
    }

    private void HardAttach(Animator targetAnim)
    {
        if (parentConstraint.sourceCount > 0)
            parentConstraint.RemoveSource(0);
        ConstraintSource constraintTargetIK = new ConstraintSource();
        constraintTargetIK.sourceTransform = targetAnim.GetBoneTransform(targetBone);
        constraintTargetIK.weight = 1;
        parentConstraint.AddSource(constraintTargetIK);
        parentConstraint.SetTranslationOffset(0, Vector3.zero);
        parentConstraint.SetRotationOffset(0, Vector3.zero);
        parentConstraint.constraintActive = true;
    }

    private void FixedUpdate()
    {
        if (targetTransform == null || rb == null) return;

        float angle = Quaternion.Angle(transform.rotation, targetTransform.rotation);
        if (angle < tolerance)
            transform.rotation = targetTransform.rotation;

        if (joint != null)
            joint.targetRotation = Quaternion.Inverse(targetTransform.rotation) * joint.connectedBody.transform.rotation;
        else
            rb.AddTorque((Quaternion.Inverse(targetTransform.rotation) * targetTransform.rotation).eulerAngles * torque);
        rb.AddForce((targetTransform.position - transform.position) * positionforce * Time.fixedDeltaTime);
    }
}
