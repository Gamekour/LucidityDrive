using UnityEngine;
using UnityEngine.Animations;

namespace LucidityDrive.Extras
{
    [RequireComponent(typeof(ParentConstraint))]
    public class BoneRoot : MonoBehaviour
    {
        [SerializeField] bool usePlayermodel = false;
        [SerializeField] HumanBodyBones targetBone;

        [Header("Force Match")]
        [SerializeField] bool useForceMatch;
        [SerializeField] float positionForce = 10;
        [SerializeField] float torque = 10;
        [SerializeField] float tolerance = 0.01f;
        private Transform targetTransform;
        private ConfigurableJoint joint;
        private Rigidbody rb;

        private ParentConstraint parentConstraint;

        private void OnEnable()
        {
            if (!useForceMatch)
                parentConstraint = GetComponent<ParentConstraint>();
            else
            {
                joint = GetComponent<ConfigurableJoint>();
                rb = GetComponent<Rigidbody>();
            }
            LucidPlayerInfo.OnAssignVismodel.AddListener(OnAssignVismodel);
            LucidPlayerInfo.OnAnimModellInitialized.AddListener(OnInitializeAnimModel);
        }

        private void OnDisable()
        {
            LucidPlayerInfo.OnAssignVismodel.RemoveListener(OnAssignVismodel);
            LucidPlayerInfo.OnAnimModellInitialized.RemoveListener(OnInitializeAnimModel);
        }

        public void OnAssignVismodel(Vismodel newModel)
        {
            if (usePlayermodel) return;

            if (!useForceMatch)
                HardAttach(newModel.anim);
            else
                targetTransform = newModel.anim.GetBoneTransform(targetBone);
        }

        public void OnInitializeAnimModel()
        {
            if (!usePlayermodel) return;

            if (!useForceMatch)
                HardAttach(LucidPlayerInfo.animationModel);
            else
                targetTransform = LucidPlayerInfo.animationModel.GetBoneTransform(targetBone);
        }

        private void HardAttach(Animator targetAnim)
        {
            if (parentConstraint.sourceCount > 0)
                parentConstraint.RemoveSource(0);
            ConstraintSource constraintTargetIK = new();
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
            rb.AddForce(positionForce * Time.fixedDeltaTime * (targetTransform.position - transform.position));
        }
    }
}