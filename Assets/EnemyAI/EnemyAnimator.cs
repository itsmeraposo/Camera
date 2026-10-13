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
}
