using UnityEngine;

namespace LucidityDrive
{
    public class AnimatorEventReciever : MonoBehaviour
    {
        public LucidAnimationModel target;

        private void OnAnimatorIK(int layerIndex)
        {
            if (target != null)
                target.AnimatorIKDelegate(layerIndex);
        }

        private void OnAnimatorMove()
        {
            if (target != null)
                target.AnimatorMoveDelegate();
        }
    }
}