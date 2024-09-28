using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class Boneroot : MonoBehaviour
{
    public bool usePlayermodel = false;
    [SerializeField] HumanBodyBones target;
    [SerializeField] ParentConstraint parentConstraint;

    private void OnEnable()
    {
        PlayerInfo.OnAssignVismodel.AddListener(Assign);
    }

    private void OnDisable()
    {
        PlayerInfo.OnAssignVismodel.RemoveListener(Assign);
    }

    public void Assign(LucidVismodel newmodel)
    {
        if (parentConstraint.sourceCount > 0)
            parentConstraint.RemoveSource(0);
        ConstraintSource constraintTargetIK = new ConstraintSource();
        constraintTargetIK.sourceTransform = newmodel.anim.GetBoneTransform(target);
        constraintTargetIK.weight = 1;
        parentConstraint.AddSource(constraintTargetIK);
        parentConstraint.SetTranslationOffset(0, Vector3.zero);
        parentConstraint.SetRotationOffset(0, Vector3.zero);
        parentConstraint.constraintActive = true;
    }
}
