using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class Boneroot : MonoBehaviour
{
    public bool usePlayermodel = false;
    [SerializeField] HumanBodyBones target;
    [SerializeField] ParentConstraint parentConstraint;

    public void Assign(Animator newAnim)
    {
        if (parentConstraint.sourceCount > 0)
            parentConstraint.RemoveSource(0);
        ConstraintSource constraintTargetIK = new ConstraintSource();
        constraintTargetIK.sourceTransform = newAnim.GetBoneTransform(target);
        constraintTargetIK.weight = 1;
        parentConstraint.AddSource(constraintTargetIK);
        parentConstraint.SetTranslationOffset(0, Vector3.zero);
        parentConstraint.SetRotationOffset(0, Vector3.zero);
        parentConstraint.constraintActive = true;
    }
}
