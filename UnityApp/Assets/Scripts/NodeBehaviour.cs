using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class NodeBehaviour : MonoBehaviour
{
    private CollektiveEngine _engine;
    private Renderer _renderer;
    [SerializeField] private double currentValue;

    public int Id { get; private set; }

    public void Initialize(int id, CollektiveEngine engine)
    {
        Id = id;
        _engine = engine;
        _renderer = GetComponent<Renderer>();
        name = $"node {Id}";
    }

    private void Update() => DisplayGradient(_engine.GetValue(Id));

    private void DisplayGradient(double value)
    {
        currentValue = value;
        var t = Mathf.InverseLerp(0f, 30f, (float)value);
        var color = _engine.IsSource(Id) ? Color.white : Color.HSVToRGB(t, 1f, 1f);
        _renderer.material.color = color;
    }
}
