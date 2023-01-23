using System;
using UnityEngine;

public class PivotController : MonoBehaviour
{
    [SerializeField] private float _sensitivity = 10f;
    [SerializeField] private float _rotSmoothness = 0.1f;
    [SerializeField] private float _xySmoothness = 0.1f;
    [SerializeField] private float _zSmoothness = 0.1f;

    private const float _xRotClamp = 90f;
    private const float _nearZero = 0.001f;

    private Vector2 _localRotation = Vector2.zero;
    private Vector3 _localPosition = Vector2.zero;
    private Vector3 _mouseInput => new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse ScrollWheel")) * _deltaSensitivity;

    private float deltaTimeConst => 100 * Time.deltaTime;
    private float _deltaSensitivity => _sensitivity * deltaTimeConst;
    private float _deltaRotSmoothness => _rotSmoothness * deltaTimeConst;
    private float _deltaXySmoothness => _xySmoothness* deltaTimeConst;
    private float _deltaZSmoothness => _zSmoothness * deltaTimeConst;

    void Update()
    {
        LeftClickInput();
        RightClickInput();
        ScrollWheelInput();
    }

    private void LeftClickInput()
    {
        if (Input.GetMouseButton(0))
        {
            _localRotation.x += _mouseInput.y;
            _localRotation.x = Mathf.Clamp(_localRotation.x, -_xRotClamp, _xRotClamp);

            _localRotation.y -= _mouseInput.x;
        }

        if (Vector2.Distance(_localRotation, transform.localEulerAngles) != 0)
        {
            float currentX = transform.localEulerAngles.x;
            float currentY = transform.localEulerAngles.y;

            currentX = Mathf.LerpAngle(currentX, _localRotation.x, _deltaRotSmoothness);
            currentY = Mathf.LerpAngle(currentY, _localRotation.y, _deltaRotSmoothness);

            transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
        }
    }

    private void RightClickInput()
    {
        if (Input.GetMouseButton(1))
        {
            var mouseMoveAxis = new Vector3(_mouseInput.x, _mouseInput.y, 0) / 10;

            _localPosition += mouseMoveAxis;
        }

        if (Vector2.Distance(new Vector2(_localPosition.x, _localPosition.y), new Vector2(transform.position.x, transform.position.y)) > _nearZero)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(_localPosition.x, _localPosition.y, transform.position.z), _deltaXySmoothness);
        }
    }

    private void ScrollWheelInput()
    {
        if (Input.GetAxis("Mouse ScrollWheel") != 0f)
        {
            var mouseScrollAxis = new Vector3(0, 0, _mouseInput.z) * 2f;

            _localPosition -= mouseScrollAxis;
        }

        if (Mathf.Abs(_localPosition.z - transform.position.z) > _nearZero)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, transform.position.y, _localPosition.z), _deltaZSmoothness);
        }
    }

    public void ResetRotation()
    {
        transform.rotation = Quaternion.identity;
        _localRotation = Vector2.zero;
    }

    public void ResetPosition(Vector3 position)
    {
        transform.position = position;
        _localPosition = transform.position;
    }
}
