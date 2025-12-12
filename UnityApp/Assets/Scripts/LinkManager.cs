using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CollektiveEngine))]
public class LinksManager : MonoBehaviour
{
    [SerializeField] private LinkBehaviour linkPrefab;

    private CollektiveEngine _engine;
    private readonly List<LinkBehaviour> _links = new();

    private void ClearLinks()
    {
        foreach (var link in _links)
        {
            if (link != null)
                Destroy(link.gameObject);
        }
        _links.Clear();
    }

    private void Start()
    {
        _engine = GetComponent<CollektiveEngine>();
        ClearLinks();
        foreach (var (a, b) in _engine.GetAllLinks())
        {
            var linkInstance = Instantiate(linkPrefab);
            linkInstance.Initialize(a, b);
            _links.Add(linkInstance);
        }
    }

    private void Update()
    {
    }
}
