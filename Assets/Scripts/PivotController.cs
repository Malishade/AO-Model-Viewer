using System;
using UnityEngine;

public class PivotController : MonoBehaviour
{
    [SerializeField] private Camera _renderCamera;
    [SerializeField] private float _sensitivity = 10f;
    [SerializeField] private float _smoothness = 0.1f;

    private const float _xRotClamp = 90f;
    private const float _nearZero = 0.001f;

    private Vector2 _localRotation = Vector2.zero;
    private Vector3 _localPosition = Vector2.zero;

    private BoundsSensitivityParams _boundsSenseParams = new BoundsSensitivityParams();

    private float _mouseInputX => Input.GetAxis("Mouse X") * _deltaSensitivity;
    private float _mouseInputY => Input.GetAxis("Mouse Y") * _deltaSensitivity;
    private float _mouseInputScroll => Input.GetAxis("Mouse ScrollWheel") * _deltaSensitivity;

    private float _deltaTimeConst => 100 * Time.deltaTime;
    private float _deltaSensitivity => _sensitivity * _deltaTimeConst;
    private float _deltaSmoothness => _smoothness * _deltaTimeConst;


    private void Update()
    {
        LeftClickInput();
        RightClickInput();
        ScrollWheelInput();
    }

    private void LeftClickInput()
    {
        if (Input.GetMouseButton(0))
        {
            _localRotation.x += _mouseInputY;
            _localRotation.x = Mathf.Clamp(_localRotation.x, -_xRotClamp, _xRotClamp);

            _localRotation.y -= _mouseInputX;
        }

        if (Vector2.Distance(_localRotation, transform.localEulerAngles) != 0)
        {
            float currentX = transform.localEulerAngles.x;
            float currentY = transform.localEulerAngles.y;

            currentX = Mathf.LerpAngle(currentX, _localRotation.x, _deltaSmoothness);
            currentY = Mathf.LerpAngle(currentY, _localRotation.y, _deltaSmoothness);

            transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
        }
    }

    private void RightClickInput()
    {
        if (Input.GetMouseButton(1))
        {
            var mouseMoveAxis = new Vector3(_mouseInputX, _mouseInputY, 0) / 10 * _boundsSenseParams.XyTranslate;

            _localPosition += mouseMoveAxis;
        }

        if (Vector2.Distance(new Vector2(_localPosition.x, _localPosition.y), new Vector2(transform.position.x, transform.position.y)) > _nearZero)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(_localPosition.x, _localPosition.y, transform.position.z), _deltaSmoothness);
        }
    }

    private void ScrollWheelInput()
    {
        if (Input.GetAxis("Mouse ScrollWheel") != 0f)
        {
            var mouseScrollAxis = Vector3.forward * _mouseInputScroll * 2f * _boundsSenseParams.ZTranslate;

            _localPosition -= mouseScrollAxis;
        }

        if (Mathf.Abs(_localPosition.z - transform.position.z) > _nearZero)
        {
            _localPosition.z = Mathf.Clamp(_localPosition.z, _renderCamera.transform.position.z, 100);
            transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, transform.position.y, _localPosition.z), _deltaSmoothness);
        }
    }

    public void UpdateData(Bounds newBounds)
    {
        transform.rotation = Quaternion.identity;
        _localRotation = Vector2.zero;

        transform.position = newBounds.center;
        _localPosition = transform.position;

        float boundsVolume = newBounds.size.x * newBounds.size.y * newBounds.size.z;
        UpdateDeltas(boundsVolume);
    }

    private void UpdateDeltas(float boundsVolume)
    {
        _boundsSenseParams.XyTranslate = boundsVolume < 1000 ? Remap(boundsVolume, 0, 1000, 0.05f, 1) : Remap(boundsVolume, 1000, 1822634, 1f, 10);
        _boundsSenseParams.ZTranslate = boundsVolume < 1000 ? Remap(boundsVolume, 0, 1000, 0.05f, 1) : Remap(boundsVolume, 1000, 1822634, 1f, 10);
    }

    public class BoundsSensitivityParams
    {
        public float XyTranslate = 1;
        public float ZTranslate = 1;
    }

    public float Remap(float input, float oldLow, float oldHigh, float newLow, float newHigh)
    {
        float t = Mathf.InverseLerp(oldLow, oldHigh, input);
        return Mathf.Lerp(newLow, newHigh, t);
    }
}