using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class NodeBehaviour : MonoBehaviour
{
    private CollektiveEngine _engine;
    private Renderer _renderer;
    private readonly Color _minColor = Color.white;
    private readonly Color _maxColor = Color.black;
    [SerializeField] private double currentValue;

    public int Id { get; private set; }

    public void Initialize(int id, CollektiveEngine engine)
    {
        Id = id;
        _engine = engine;
        _renderer = GetComponent<Renderer>();
    }

    private void Update() => DisplayGradient(_engine.GetValue(Id));

    private void DisplayGradient(double value)
    {
        currentValue = value;
        var t = Mathf.InverseLerp(0f, 30f, (float)value);
        var color = Color.Lerp(_minColor, _maxColor, t);
        _renderer.material.color = color;
    }
}
