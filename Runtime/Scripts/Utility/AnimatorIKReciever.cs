using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorIKReciever : MonoBehaviour
{
    public LucidAnimationModel target;

    private void OnAnimatorIK(int layerIndex)
    {
        if(target != null)
            target.AnimatorIKDelegate(layerIndex);
    }

    private void OnAnimatorMove()
    {
        if (target != null)
            target.AnimatorMoveDelegate();
    }
}