using UnityEngine;

public class ModelController : MonoBehaviour
{
    [SerializeField] private float _mouseSpeed = 10f; 
    [SerializeField] private ModelViewer _modelViewer;

    private Vector2 _mouseMoveAxis => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    private float _mouseScrollAxis => Input.GetAxis("Mouse ScrollWheel");

    void Update()
    {
        if (_modelViewer.CurrentModel == null)
            return;

        if (Input.GetMouseButton(0))
        {
            var mouseMoveAxis = _mouseMoveAxis * _mouseSpeed;

            _modelViewer.PivotPoint.transform.Rotate(0f, -mouseMoveAxis.x, 0f, Space.World);
            _modelViewer.PivotPoint.transform.Rotate(mouseMoveAxis.y, 0f, 0f, Space.Self);
        }

        if (Input.GetMouseButton(1))
        {
            var mouseMoveAxis = _mouseMoveAxis * _mouseSpeed / 20;

            _modelViewer.PivotPoint.transform.Translate(0f, mouseMoveAxis.y, 0f, Space.World);
            _modelViewer.PivotPoint.transform.Translate(mouseMoveAxis.x, 0f, 0f, Space.World);
        }

        if (Input.GetAxis("Mouse ScrollWheel") != 0f)
        {
            var mouseScrollAxis = _mouseScrollAxis * _mouseSpeed;

            _modelViewer.PivotPoint.transform.Translate(0, 0, -mouseScrollAxis, Space.World);
        }
    }
}
