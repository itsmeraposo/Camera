using UnityEngine;

public class PortaOclusao : MonoBehaviour
{
    public OcclusionPortal portal;
    public HingeJoint dobradica;
    public float anguloMinimo = 10f;
    public float anguloAtual;   // só para ver em Play

    Quaternion rotFechada;

    void Start()
    {
        // a porta deve estar fechada quando o jogo começa
        rotFechada = dobradica.transform.localRotation;
    }

    void Update()
    {
        anguloAtual = Quaternion.Angle(rotFechada, dobradica.transform.localRotation);
        portal.open = anguloAtual > anguloMinimo;
    }
}
