using UnityEngine;

// INIMIGO INVESTIDA: configuração da investida.
// Vai no objeto PAI do inimigo (junto com EnemyBrain, EnemyMovement, EnemySensor...).
// No EnemyBrain, escolha Type = Charger.
// NÃO coloque o Attack.cs (corpo a corpo) neste inimigo.
//
// Como funciona: vê o player -> para e AVISA (windup) -> corre em linha reta na direção travada
// -> se acertar o player causa dano; se bater na parede fica ATORDOADO (hora do player atacar).
//
// Animator (todos opcionais, não dão erro se faltarem):
//   Trigger "Windup"  - animação de preparar (raspar o chão, rugir)
//   Bool    "Charging" - animação de correr rápido (enquanto investe)
//   Trigger "Stun"    - animação de atordoado (bateu na parede)
public class ChargerAttack : MonoBehaviour
{
    [Header("Quando investir")]
    [Tooltip("Só investe se o player estiver a menos disso (metros)")]
    public float maxChargeDistance = 10f;
    public float chargeCooldown = 4f;

    [Header("Aviso (windup)")]
    [Tooltip("Tempo parado se preparando. É a chance do player desviar")]
    public float windupTime = 0.9f;
    [Tooltip("Nos últimos segundos do aviso a direção trava (o player desvia saindo da linha)")]
    public float lockAimTime = 0.25f;
    [Tooltip("Opcional: objeto que aparece durante o aviso (um '!' vermelho, partículas...)")]
    public GameObject telegraphObject;

    [Header("Investida")]
    public float chargeSpeed = 12f;
    [Tooltip("Distância máxima que ele corre (metros)")]
    public float chargeDistance = 9f;
    public float damage = 20f;
    [Tooltip("Raio da área de acerto, à frente do inimigo")]
    public float hitRadius = 0.9f;
    public float hitForward = 0.8f;
    [Tooltip("Empurrão no player (só funciona se ele tiver Rigidbody não-kinematic). 0 = sem empurrão")]
    public float knockbackForce = 4f;

    [Header("Depois da investida")]
    [Tooltip("Tempo parado depois de errar o golpe")]
    public float recoverTime = 0.8f;
    [Tooltip("Tempo atordoado ao bater na parede")]
    public float stunTime = 2.5f;

    [Header("Nomes no Animator")]
    public string windupTrigger = "Windup";
    public string chargingBool = "Charging";
    public string stunTrigger = "Stun";

    private float lastChargeEnd = -999f;

    public bool CooldownReady => Time.time - lastChargeEnd >= chargeCooldown;

    public void NotifyChargeEnd()
    {
        lastChargeEnd = Time.time;
    }

    public void SetTelegraph(bool on)
    {
        if (telegraphObject != null)
            telegraphObject.SetActive(on);
    }

    void Start()
    {
        SetTelegraph(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxChargeDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward * hitForward + Vector3.up * 0.5f, hitRadius);
    }
}
