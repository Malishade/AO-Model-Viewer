using System;
using UnityEngine;

public class PivotController : MonoBehaviour
{
    [SerializeField] public Camera RenderCamera;
    [SerializeField] private float _sensitivity = 10f;
    [SerializeField] private float _smoothness = 10f;

    private const float _xRotClamp = 90f;
    private const float _nearZero = 0.001f;

    private Vector2 _localRotation = Vector2.zero;
    private Vector3 _localPosition = Vector2.zero;

    private float _mouseInputX => Input.GetAxis("Mouse X") * _deltaSensitivity;
    private float _mouseInputY => Input.GetAxis("Mouse Y") * _deltaSensitivity;
    private float _mouseInputScroll => Input.GetAxis("Mouse ScrollWheel") * _deltaSensitivity;

    private float _boundsSensitivity;

    private float _sensitivityConst = 0.25f;
    private float _deltaSensitivity => _sensitivity * _sensitivityConst;
    private float _deltaSmoothness => _smoothness * Time.deltaTime;
    [HideInInspector] public bool DisableMouseInput = false;

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

        var mouseMoveAxis = new Vector3(_mouseInputX, _mouseInputY, 0) / 10 * _boundsSensitivity;

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

        var mouseScrollAxis = Vector3.forward * _mouseInputScroll * 2f * _boundsSensitivity;

        _localPosition -= mouseScrollAxis;
    }

    private void ScrollWheelTravel()
    {
        if (!(Mathf.Abs(_localPosition.z - transform.position.z) > _nearZero))
            return;

        _localPosition.z = Mathf.Clamp(_localPosition.z, RenderCamera.transform.position.z, 100);
        transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, transform.position.y, _localPosition.z), _deltaSmoothness);
    }
    public void UpdateData(Bounds newBounds)
    {
        transform.rotation = Quaternion.identity;
        _localRotation = Vector2.zero;

        transform.position = newBounds.center;
        _localPosition = transform.position;

        float boundsVolume = newBounds.size.x * newBounds.size.y * newBounds.size.z;
        UpdateBoundsSensitivity(boundsVolume);
    }

    private void UpdateBoundsSensitivity(float boundsVolume)
    {
        Quaternion mapValues =     
            boundsVolume < 0.1 ?
            new Quaternion(0, 0.1f, 0.01f, 0.35f) :
            boundsVolume < 10 ?
            new Quaternion(0.1f, 10, 0.35f, 0.5f) :
            boundsVolume < 200 ?
            new Quaternion(10, 200, 0.5f, 0.75f) :
            boundsVolume < 1000 ?
            new Quaternion(200, 1000, 0.75f, 2f) :
            boundsVolume < 10000 ?
            new Quaternion(1000, 10000, 2f, 4f) :
            boundsVolume < 40000 ?
            new Quaternion(10000, 40000, 4f, 6f):
            new Quaternion(40000, 1822634, 6f, 10f);

        _boundsSensitivity = Remap(boundsVolume, mapValues.x, mapValues.y, mapValues.z, mapValues.w);
    }

    public float Remap(float input, float oldLow, float oldHigh, float newLow, float newHigh)
    {
        float t = Mathf.InverseLerp(oldLow, oldHigh, input);
        return Mathf.Lerp(newLow, newHigh, t);
    }
}