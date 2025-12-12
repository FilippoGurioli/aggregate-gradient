using UnityEngine;
using UnityEngine.InputSystem;

public class FlyCamera : MonoBehaviour
{
    [Header("Look")]
    public float lookSensitivity = 0.15f;

    [Header("Move")]
    public float moveSpeed = 10f;
    public float fastMultiplier = 3f;
    public float slowMultiplier = 0.25f;

    private Vector2 _lookDelta;

    void Update()
    {
        if (!Mouse.current.rightButton.isPressed)
            return;
        _lookDelta = Mouse.current.delta.ReadValue();
        float mx = _lookDelta.x * lookSensitivity;
        float my = _lookDelta.y * lookSensitivity;
        var e = transform.eulerAngles;
        e.x -= my;
        e.y += mx;
        transform.eulerAngles = e;
        Vector3 dir = Vector3.zero;
        if (Keyboard.current.wKey.isPressed) dir += Vector3.forward;
        if (Keyboard.current.sKey.isPressed) dir += Vector3.back;
        if (Keyboard.current.dKey.isPressed) dir += Vector3.right;
        if (Keyboard.current.aKey.isPressed) dir += Vector3.left;
        if (Keyboard.current.eKey.isPressed) dir += Vector3.up;
        if (Keyboard.current.qKey.isPressed) dir += Vector3.down;
        if (dir.sqrMagnitude == 0f) return;
        float speed = moveSpeed;
        if (Keyboard.current.leftShiftKey.isPressed) speed *= fastMultiplier;
        if (Keyboard.current.leftCtrlKey.isPressed) speed *= slowMultiplier;
        transform.Translate(dir.normalized * speed * Time.deltaTime, Space.Self);
    }
}
