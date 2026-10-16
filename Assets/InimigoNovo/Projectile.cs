using UnityEngine;

// Projétil do inimigo atirador.
// Vai no PREFAB do projétil (uma esfera pequena). NÃO precisa de collider nem de Rigidbody:
// ele detecta os acertos sozinho com SphereCast (não "atravessa" o rato mesmo sendo pequeno e rápido).
public class Projectile : MonoBehaviour
{
    [Tooltip("Tamanho da área de acerto do projétil")]
    public float radius = 0.15f;
    [Tooltip("Some sozinho depois de tantos segundos")]
    public float lifeTime = 6f;
    public LayerMask hitMask = ~0;

    [Header("Efeito visual")]
    [Tooltip("Giro enquanto voa, em graus por segundo (X, Y, Z). Ex: latinha girando = 0, 0, 720. Zero = não gira.")]
    public Vector3 spin = new Vector3(0f, 0f, 720f);

    private Vector3 direction = Vector3.forward;
    private float speed = 10f;
    private float damage = 8f;
    private Transform owner;

    private static readonly RaycastHit[] buffer = new RaycastHit[16];

    public void Init(Vector3 dir, float projectileSpeed, float projectileDamage, Transform shooter)
    {
        direction = dir.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        owner = shooter;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        float step = speed * Time.deltaTime;

        int count = Physics.SphereCastNonAlloc(transform.position, radius, direction, buffer, step, hitMask, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < count; i++)
        {
            Collider col = buffer[i].collider;

            // Ignora quem atirou e outros inimigos (o tiro passa por eles)
            if (owner != null && col.transform.IsChildOf(owner))
                continue;

            if (col.GetComponentInParent<EnemyBrain>() != null)
                continue;

            if (buffer[i].distance < best)
            {
                best = buffer[i].distance;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            PlayerHealth health = buffer[bestIndex].collider.GetComponentInParent<PlayerHealth>();

            if (health != null)
            {
                health.TakeDamage(damage);
                Debug.Log("Player atingido pelo projétil!");
            }

            Destroy(gameObject);
            return;
        }

        transform.position += direction * step;

        if (spin != Vector3.zero)
            transform.Rotate(spin * Time.deltaTime, Space.Self);
    }
}
