using UnityEngine;

public class AttackLockSMB : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("IsAttacking", true);
        // Limpia triggers por si quedaron pendientes
        animator.ResetTrigger("Attack1");
        animator.ResetTrigger("Attack2");
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("IsAttacking", false);
    }
}
