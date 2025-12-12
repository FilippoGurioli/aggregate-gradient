using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LinkBehaviour : MonoBehaviour
{
    private NodeBehaviour _a;
    private NodeBehaviour _b;
    private LineRenderer _line;

    public (NodeBehaviour, NodeBehaviour) Sides => (_a, _b);

    public void Initialize(NodeBehaviour a, NodeBehaviour b)
    {
        _a = a;
        _b = b;
        _line = GetComponent<LineRenderer>();
        _line.widthMultiplier = 0.2f;
        _line.material.SetColor("_BaseColor", Color.black);
        _line.material.SetColor("_Tint", Color.black);
        _line.generateLightingData = true;
        UpdatePositions();
    }

    private void LateUpdate()
    {
        if (_a == null || _b == null)
        {
            Destroy(gameObject);
            return;
        }
        UpdatePositions();
    }

    private void UpdatePositions()
    {
        _line.SetPosition(0, _a.transform.position);
        _line.SetPosition(1, _b.transform.position);
    }

    public override bool Equals(object other)
    {
        if (other is not LinkBehaviour) return false;
        var link = (LinkBehaviour)other;
        return (Sides.Item1 == link.Sides.Item1 && Sides.Item2 == link.Sides.Item2) ||
          (Sides.Item1 == link.Sides.Item2 && Sides.Item2 == link.Sides.Item1);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashA = _a != null ? _a.GetHashCode() : 0;
            int hashB = _b != null ? _b.GetHashCode() : 0;
            return hashA ^ hashB;
        }
    }
}
