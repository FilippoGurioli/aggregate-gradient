using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class NodeBehaviour : MonoBehaviour
{
    private CollektiveEngine _engine;
    private Renderer _renderer;
    private readonly Color _minColor = Color.blue;
    private readonly Color _maxColor = Color.red;

    public int Id { get; private set; }

    public void Initialize(int id, CollektiveEngine engine)
    {
        Id = id;
        _engine = engine;
        _renderer = GetComponent<Renderer>();
    }

    private void Update() => DisplayGradient(_engine.GetValue(Id));

    private void DisplayGradient(int value)
    {
        if (Id == 90) Debug.Log(value);
        var t = Mathf.InverseLerp(0f, 6, value);
        var color = Color.Lerp(_minColor, _maxColor, t);
        _renderer.material.color = color;
    }
}
