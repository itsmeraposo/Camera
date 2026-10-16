using UnityEngine;

// Colocar no MESMO objeto que tem o Animator do modelo (o filho com o modelo do inimigo).
// Os Animation Events só chegam em scripts que estão no mesmo objeto do Animator.
// Este script repassa os eventos para os scripts do objeto PAI:
//   AttackStart / AttackEnd -> Attack (inimigo corpo a corpo)
//   ThrowRelease            -> RangedAttack (inimigo atirador)
public class EnemyAnimationEvents : MonoBehaviour
{
    private Attack attack;
    private RangedAttack ranged;

    void Awake()
    {
        attack = GetComponentInParent<Attack>();
        ranged = GetComponentInParent<RangedAttack>();

        if (attack == null && ranged == null)
            Debug.LogWarning("[EnemyAnimationEvents] Não achei Attack nem RangedAttack nos objetos pais de " + name);
    }

    // Animation Event (corpo a corpo): frame em que o golpe começa a causar dano
    public void AttackStart()
    {
        if (attack != null)
            attack.AttackStart();
    }

    // Animation Event (corpo a corpo): fim do golpe
    public void AttackEnd()
    {
        if (attack != null)
            attack.AttackEnd();
    }

    // Animation Event (atirador): frame em que a mão solta a latinha
    public void ThrowRelease()
    {
        if (ranged != null)
            ranged.ThrowRelease();
    }
}
