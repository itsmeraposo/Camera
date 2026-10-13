using UnityEngine;

public class TravarHips : MonoBehaviour
{
    [Tooltip("Osso Hips do rato. Se vazio, procura por nome.")]
    public Transform hips;

    [Header("Eixos que ficam travados (posição local)")]
    public bool travarX = true;
    public bool travarY = false;
    public bool travarZ = true;

    private Vector3 inicial;

    void Start()
    {
        if (hips == null)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                if (t.name.EndsWith("Hips")) { hips = t; break; }
            }
        }
        if (hips != null) inicial = hips.localPosition;
    }

    void LateUpdate()
    {
        if (hips == null) return;
        Vector3 p = hips.localPosition;
        if (travarX) p.x = inicial.x;
        if (travarY) p.y = inicial.y;
        if (travarZ) p.z = inicial.z;
        hips.localPosition = p;
    }
}