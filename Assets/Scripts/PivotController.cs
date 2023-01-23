using UnityEngine;

public class PivotController : MonoBehaviour
{
    [SerializeField] private float _sensitivity = 10f;
    [SerializeField] private float _rotSmoothness = 0.1f;
    [SerializeField] private float _moveSmoothness = 0.1f;

    private Vector2 _mouseMoveAxis => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * _sensitivity * 100 * Time.deltaTime;
    
    private float _mouseScrollAxis => Input.GetAxis("Mouse ScrollWheel");
 
    private Vector2 _localRotation = Vector2.zero;
    private Vector2 _localPosition = Vector2.zero;

    private float _xRotClamp = 90f;

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            _localRotation.x -= _mouseMoveAxis.y;
            _localRotation.x = Mathf.Clamp(_localRotation.x, -_xRotClamp, _xRotClamp);

            _localRotation.y += _mouseMoveAxis.x;

            float currentX = transform.localEulerAngles.x;
            float currentY = transform.localEulerAngles.y;

            currentX = Mathf.LerpAngle(currentX, _localRotation.x, _rotSmoothness * 100 * Time.deltaTime);
            currentY = Mathf.LerpAngle(currentY, _localRotation.y, _rotSmoothness * 100 * Time.deltaTime);

            transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
        }


        if (Input.GetMouseButton(1))
        {
            var mouseMoveAxis = _mouseMoveAxis / 10;

            _localPosition += mouseMoveAxis;

            transform.position = Vector3.Lerp(transform.position, new Vector3(_localPosition.x, _localPosition.y, transform.position.z), _moveSmoothness * 100 * Time.deltaTime);
        }

        if (Input.GetAxis("Mouse ScrollWheel") != 0f)
        {
            var mouseScrollAxis = _mouseScrollAxis * 5f;

            transform.Translate(Vector3.forward * -mouseScrollAxis, Space.World);
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
