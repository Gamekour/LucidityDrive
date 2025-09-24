using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerWeightOverride : StateMachineBehaviour
{
    public int[] layerIndices;
    public float[] layerWeights;
    private float layerRef = 0;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        for (int i = 0; i < layerIndices.Length; i++)
        {
            LucidAnimationModel.layerOverrides[layerIndices[i]] = true;
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        for (int i = 0; i < layerIndices.Length; i++)
        {
            float current = animator.GetLayerWeight(layerIndices[i]);
            float weight = Mathf.SmoothDamp(current, layerWeights[i], ref layerRef, 0.5f);
            animator.SetLayerWeight(layerIndices[i], weight);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        for (int i = 0; i < layerIndices.Length; i++)
        {
            LucidAnimationModel.layerOverrides[layerIndices[i]] = false;
        }
    }
}
