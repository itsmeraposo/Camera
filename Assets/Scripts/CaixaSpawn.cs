using UnityEngine;

public class CaixaSpawn : MonoBehaviour
{
    [Header("Spawn de Inimigos")]
    public GameObject inimigoPrefab;
    public int quantidadeInimigos = 2;
    public float raioSpawn = 2f;

    private bool jaSpawnou = false;

    // Chamado explicitamente pelo StatusThings quando a caixa realmente morre por dano
    public void SpawnInimigos()
    {
        if (jaSpawnou) return;
        jaSpawnou = true;

        if (inimigoPrefab == null) return;

        for (int i = 0; i < quantidadeInimigos; i++)
        {
            Vector2 posAleatoria = Random.insideUnitCircle.normalized * raioSpawn;
            Vector3 spawnPos = transform.position + new Vector3(posAleatoria.x, 0f, posAleatoria.y);

            Instantiate(inimigoPrefab, spawnPos, Quaternion.identity);
        }
    }
}