using UnityEngine;
using UnityEngine.InputSystem;

public class RuntimeTranslateTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject gizmoRoot;
    [SerializeField] private Collider xHandle;
    [SerializeField] private Collider yHandle;
    [SerializeField] private Collider zHandle;

    [Header("Raycast")]
    [SerializeField] private LayerMask selectableMask = ~0;
    [SerializeField] private LayerMask gizmoMask = ~0;
    [SerializeField] private float rayDistance = 5000f;

    [Header("Gizmo")]
    [SerializeField] private float gizmoScaleFactor = 0.1f;
    [SerializeField] private float minGizmoScale = 0.25f;
    [SerializeField] private float maxGizmoScale = 10f;

    private Transform _selected;
    private Camera cam;

    private enum Axis { None, X, Y, Z }
    private Axis _activeAxis = Axis.None;

    private Plane _dragPlane;
    private Vector3 _startTargetPos;
    private Vector3 _startHitPointOnPlane;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        if (gizmoRoot != null)
            gizmoRoot.SetActive(false);
    }

    private void Update()
    {
        if (cam == null) return;
        UpdateGizmoTransform();
        if (_activeAxis != Axis.None)
        {
            if (Mouse.current.leftButton.isPressed)
                DragSelectedAlongAxis();
            else
                _activeAxis = Axis.None;
            return;
        }
        if (Mouse.current.rightButton.isPressed)
            return;
        if (Mouse.current.leftButton.wasPressedThisFrame && TryBeginDragOnHandle())
            return;
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TrySelectObjectUnderMouse();
    }

    private void UpdateGizmoTransform()
    {
        if (gizmoRoot == null) return;
        if (_selected == null)
        {
            if (gizmoRoot.activeSelf) gizmoRoot.SetActive(false);
            return;
        }
        if (!gizmoRoot.activeSelf) gizmoRoot.SetActive(true);
        gizmoRoot.transform.position = _selected.position;
        gizmoRoot.transform.rotation = Quaternion.identity;
        float dist = Vector3.Distance(cam.transform.position, gizmoRoot.transform.position);
        float s = Mathf.Clamp(dist * gizmoScaleFactor, minGizmoScale, maxGizmoScale);
        gizmoRoot.transform.localScale = Vector3.one * s;
    }

    private void TrySelectObjectUnderMouse()
    {
        var ray = ScreenRay();
        if (Physics.Raycast(ray, out var hit, rayDistance, selectableMask, QueryTriggerInteraction.Ignore))
        {
            _selected = hit.transform;
            Debug.Log($"[{name}] selected {_selected.name}", this);
        }
        else
            _selected = null;
    }

    private bool TryBeginDragOnHandle()
    {
        if (_selected == null) return false;
        var ray = ScreenRay();
        if (!Physics.Raycast(ray, out var hit, rayDistance, gizmoMask, QueryTriggerInteraction.Ignore))
            return false;
        if (hit.collider == xHandle) _activeAxis = Axis.X;
        else if (hit.collider == yHandle) _activeAxis = Axis.Y;
        else if (hit.collider == zHandle) _activeAxis = Axis.Z;
        else return false;
        _startTargetPos = _selected.position;
        Vector3 axisDir = AxisDirectionWorld(_activeAxis);
        Vector3 planeNormal = Vector3.Cross(axisDir, cam.transform.forward);
        if (planeNormal.sqrMagnitude < 1e-6f)
            planeNormal = Vector3.Cross(axisDir, cam.transform.up);
        planeNormal.Normalize();
        _dragPlane = new Plane(planeNormal, _startTargetPos);
        if (!_dragPlane.Raycast(ray, out float enter))
        {
            _activeAxis = Axis.None;
            return false;
        }
        _startHitPointOnPlane = ray.GetPoint(enter);
        return true;
    }

    private void DragSelectedAlongAxis()
    {
        if (_selected == null)
        {
            _activeAxis = Axis.None;
            return;
        }
        var ray = ScreenRay();
        if (!_dragPlane.Raycast(ray, out float enter))
            return;
        Vector3 hitPoint = ray.GetPoint(enter);
        Vector3 deltaOnPlane = hitPoint - _startHitPointOnPlane;
        Vector3 axisDir = AxisDirectionWorld(_activeAxis);
        float amount = Vector3.Dot(deltaOnPlane, axisDir);
        _selected.position = _startTargetPos + axisDir * amount;
    }

    private Ray ScreenRay()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        return cam.ScreenPointToRay(mousePos);
    }

    private static Vector3 AxisDirectionWorld(Axis axis)
    {
        return axis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            Axis.Z => Vector3.forward,
            _ => Vector3.zero
        };
    }
}
