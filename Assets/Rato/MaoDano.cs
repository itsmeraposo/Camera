using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaoDano : MonoBehaviour
{
    public float damage = 10f;

    private bool ativo;
    private readonly HashSet<Collider> jaAcertou = new HashSet<Collider>();
    private PlayerFlask playerFlask;

    void Start()
    {
        playerFlask = GetComponentInParent<PlayerFlask>();
        GetComponent<Collider>().isTrigger = true;
    }

    public void AbrirJanela(float dano, float atraso, float duracao)
    {
        StopAllCoroutines();
        StartCoroutine(Janela(dano, atraso, duracao));
    }

    IEnumerator Janela(float dano, float atraso, float duracao)
    {
        damage = dano;
        ativo = false;
        yield return new WaitForSeconds(atraso);
        jaAcertou.Clear();
        ativo = true;
        yield return new WaitForSeconds(duracao);
        ativo = false;
    }

    void OnTriggerStay(Collider other)
    {
        if (!ativo || !jaAcertou.Add(other)) return;

        EnemyStatus enemy = other.GetComponentInParent<EnemyStatus>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            playerFlask?.OnAcertarInimigo();
            return;
        }

        StatusThings thing = other.GetComponentInParent<StatusThings>();
        if (thing != null) { thing.TakeDamage(damage); return; }

        ExplosiveCrate crate = other.GetComponentInParent<ExplosiveCrate>();
        if (crate != null) { crate.TakeDamage(damage); return; }
    }
}