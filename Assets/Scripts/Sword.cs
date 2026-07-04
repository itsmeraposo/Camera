using UnityEngine;

public class Sword : MonoBehaviour
{
    public float damage = 10f;
    private Animator animator;
    private PlayerFlask playerFlask;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerFlask = GetComponentInParent<PlayerFlask>();
    }

    public void Attack()
    {
        PlayAnimation("AnimacaoFaca 0");
    }

    //  COMBO: toca o estado de animação correspondente ao golpe atual do combo
    // transitionTime = tempo do "blend" suave entre a animação atual e a nova (em segundos)
    public void PlayAnimation(string stateName, float transitionTime = 0.1f)
    {
        if (animator != null)
        {
            // CrossFade mistura suavemente a animação atual com a nova, ao invés de cortar seco
            animator.CrossFade(stateName, transitionTime, 0, 0f);
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        Debug.Log("Collidiu com " + collision.gameObject.name);

        // 1. INIMIGO
        EnemyStatus enemy = collision.gameObject.GetComponent<EnemyStatus>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);

            //  FLASK: cura o player se o efeito de fúria estiver ativo
            playerFlask?.OnAcertarInimigo();
            return;
        }

        // 2. CAIXA DO BEM
        StatusThings thing = collision.gameObject.GetComponent<StatusThings>();
        if (thing != null)
        {
            thing.TakeDamage(damage);
            return;
        }

        // 3. CAIXA EXPLOSIVA
        ExplosiveCrate crate = collision.gameObject.GetComponent<ExplosiveCrate>();
        if (crate != null)
        {
            crate.TakeDamage(damage);
            return;
        }

        // NADA MAIS ACONTECE! SEM SOM DE HIT, SEM DANO EM PAREDES/CHÃO.
    }
}
