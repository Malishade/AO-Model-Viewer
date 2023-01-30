using System;
using UnityEngine;

public class PivotController : MonoBehaviour
{
    [SerializeField] public Camera RenderCamera;
    [HideInInspector] public bool DisableMouseInput = false;
    [SerializeField] private float _sensitivity = 10f;
    [SerializeField] private float _smoothness = 10f;

    private const float _xRotClamp = 90f;
    private const float _nearZero = 0.001f;

    private Vector2 _localRotation = Vector2.zero;
    private Vector3 _localPosition = Vector2.zero;

    private float _mouseInputX => Input.GetAxis("Mouse X") * _deltaSensitivity;
    private float _mouseInputY => Input.GetAxis("Mouse Y") * _deltaSensitivity;
    private float _mouseInputScroll => Input.GetAxis("Mouse ScrollWheel") * _deltaSensitivity;

    private float _sensitivityConst = 0.25f;
    private float _deltaSensitivity => _sensitivity * _sensitivityConst;
    private float _deltaSmoothness => _smoothness * Time.deltaTime;

    private void Update()
    {
        if (!DisableMouseInput)
        {
            LeftClickInput();
            RightClickInput();
            ScrollWheelInput();
        }

        LeftClickTravel();
        RightClickTravel();
        ScrollWheelTravel();
    }

    private void LeftClickInput()
    {
        if (!Input.GetMouseButton(0))
            return;

        _localRotation.x += _mouseInputY;
        _localRotation.x = Mathf.Clamp(_localRotation.x, -_xRotClamp, _xRotClamp);
        _localRotation.y -= _mouseInputX;
    }

    private void LeftClickTravel()
    {
        if (!(Vector2.Distance(_localRotation, transform.localEulerAngles) != 0))
            return;

        float currentX = transform.localEulerAngles.x;
        float currentY = transform.localEulerAngles.y;

        currentX = Mathf.LerpAngle(currentX, _localRotation.x, _deltaSmoothness);
        currentY = Mathf.LerpAngle(currentY, _localRotation.y, _deltaSmoothness);

        transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    private void RightClickInput()
    {
        if (!Input.GetMouseButton(1))
            return;

        var mouseMoveAxis = new Vector3(_mouseInputX, _mouseInputY, 0) / 10;

        _localPosition += mouseMoveAxis;
    }

    private void RightClickTravel()
    {
        if (!(Vector2.Distance(new Vector2(_localPosition.x, _localPosition.y), new Vector2(transform.position.x, transform.position.y)) > _nearZero))
            return;

        transform.position = Vector3.Lerp(transform.position, new Vector3(_localPosition.x, _localPosition.y, transform.position.z), _deltaSmoothness);
    }

    private void ScrollWheelInput()
    {
        if (!(Input.GetAxis("Mouse ScrollWheel") != 0f))
            return;

        var mouseScrollAxis = Vector3.forward * _mouseInputScroll * 2f;

        _localPosition -= mouseScrollAxis;
    }

    private void ScrollWheelTravel()
    {
        if (!(Mathf.Abs(_localPosition.z - transform.position.z) > _nearZero))
            return;

        _localPosition.z = Mathf.Clamp(_localPosition.z, RenderCamera.transform.position.z, 100);
        transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, transform.position.y, _localPosition.z), _deltaSmoothness);
    }

    public void UpdateData(Bounds modelBounds)
    {
        transform.position = Vector3.forward * modelBounds.extents.z;
        transform.rotation = Quaternion.identity;

        _localRotation = Vector2.zero;
        _localPosition = transform.position;
    }
}