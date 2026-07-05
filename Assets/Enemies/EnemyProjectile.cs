using UnityEngine;

// Projétil: viaja pra frente e causa dano ao acertar o player.
// Se "teleguiado" estiver marcado, ele corrige a rota em direção ao player
// durante o voo (não só mira no lançamento) — dificulta desviar depois do tiro sair.
public class EnemyProjectile : MonoBehaviour
{
    public float dano = 10f;
    public float velocidade = 12f;
    public float tempoDeVida = 5f; // auto-destrói se não acertar nada

    [Header("Perseguição (opcional)")]
    public bool teleguiado = false;
    public float forcaCurva = 90f; // graus por segundo que ele consegue corrigir a rota

    private Transform alvo;

    void Start()
    {
        Destroy(gameObject, tempoDeVida);

        if (teleguiado)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                alvo = player.transform;
        }
    }

    void Update()
    {
        if (teleguiado && alvo != null)
        {
            Vector3 direcaoAlvo = (alvo.position + Vector3.up) - transform.position;
            if (direcaoAlvo.sqrMagnitude > 0.001f)
            {
                Quaternion rotacaoAlvo = Quaternion.LookRotation(direcaoAlvo);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, rotacaoAlvo, forcaCurva * Time.deltaTime);
            }
        }

        transform.position += transform.forward * velocidade * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(dano);
            }

            Destroy(gameObject);
            return;
        }

        // Bateu em outra coisa que não é o próprio inimigo (parede, caixa, etc.) — destrói
        if (!other.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}
