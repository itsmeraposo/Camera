using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    public Animator animator;
    public string speedParameter = "Speed";
    public string alertParameter = "Alert";

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void SetSpeed(float speed)
    {
        if (animator != null)
            animator.SetFloat(speedParameter, speed, 0.1f, Time.deltaTime);
    }

    public void SetAlert(bool alert)
    {
        if (animator != null)
            animator.SetBool(alertParameter, alert);
    }

    // Dispara um Trigger só se ele existir no Animator (sem erro se não existir)
    public void SetTrigger(string triggerName)
    {
        if (Has(triggerName, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(triggerName);
    }

    // Liga/desliga um Bool só se ele existir no Animator
    public void SetBool(string boolName, bool value)
    {
        if (Has(boolName, AnimatorControllerParameterType.Bool))
            animator.SetBool(boolName, value);
    }

    bool Has(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(paramName))
            return false;

        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == paramName && p.type == type)
                return true;

        return false;
    }
}
