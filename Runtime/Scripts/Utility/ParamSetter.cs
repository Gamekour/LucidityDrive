using UnityEngine;

public class ParamSetter : StateMachineBehaviour
{
    enum ParamType { Bool, Int, Float, Trigger }
    [SerializeField] ParamType paramType = ParamType.Bool;
    [SerializeField] string paramName;
    [SerializeField] bool onExit = false;
    [Header("If using bool, use 0 or 1")]
    [SerializeField] float value;
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!onExit)
        {
            ParamSet(animator);
        }
    }

    private void ParamSet(Animator animator)
    {
        switch (paramType)
        {
            case ParamType.Bool:
                animator.SetBool(paramName, value != 0);
                break;
            case ParamType.Float:
                animator.SetFloat(paramName, value);
                break;
            case ParamType.Int:
                animator.SetInteger(paramName, Mathf.RoundToInt(value));
                break;
            case ParamType.Trigger:
                animator.SetTrigger(paramName);
                break;
        }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (onExit)
        {
            ParamSet(animator);
        }
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
