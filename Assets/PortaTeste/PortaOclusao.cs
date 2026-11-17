using UnityEngine;

public class PortaOclusao : MonoBehaviour
{
    public OcclusionPortal portal;
    public HingeJoint dobradica;

    void Update()
    {
        portal.open = Mathf.Abs(dobradica.angle) > 3f;
    }
}
