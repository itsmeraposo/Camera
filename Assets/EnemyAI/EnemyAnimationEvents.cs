using UnityEngine;

// Colocar no MESMO objeto que tem o Animator do modelo (o filho com o modelo do inimigo).
// Os Animation Events (AttackStart / AttackEnd) só chegam em scripts que estão no mesmo objeto do Animator.
// Este script repassa os eventos para o Attack, que fica no objeto pai do inimigo.
public class EnemyAnimationEvents : MonoBehaviour
{
    private Attack attack;

    void Awake()
    {
        attack = GetComponentInParent<Attack>();

        if (attack == null)
            Debug.LogWarning("[EnemyAnimationEvents] Não achei o script Attack nos objetos pais de " + name);
    }

    // Animation Event: frame em que o golpe começa a causar dano
    public void AttackStart()
    {
        if (attack != null)
            attack.AttackStart();
    }

    // Animation Event: fim do golpe
    public void AttackEnd()
    {
        if (attack != null)
            attack.AttackEnd();
    }
}
