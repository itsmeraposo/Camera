using UnityEngine;

public static class NoiseSystem
{
    public static void EmitNoise(Vector3 position, float radius, LayerMask enemyLayer)
    {
        if (radius <= 0f)
            return;

        Collider[] hits = Physics.OverlapSphere(position, radius, enemyLayer);

        foreach (Collider col in hits)
        {
            EnemyBrain brain = col.GetComponentInParent<EnemyBrain>();

            if (brain != null)
                brain.ReceiveNoise(position);
        }
    }
}
