using UnityEngine;

public class ModelController : MonoBehaviour
{
    [SerializeField] private float _mouseSpeed = 10f; 
    [SerializeField] private ModelViewer _modelViewer;

    void Update()
    {
        if (_modelViewer.CurrentModel == null)
            return;

        if (Input.GetMouseButton(0))
        {
            float rotX = Input.GetAxis("Mouse X") * _mouseSpeed;
            float rotY = Input.GetAxis("Mouse Y") * _mouseSpeed;

            _modelViewer.PivotPoint.transform.Rotate(0f, -rotX, 0f, Space.World);
            _modelViewer.PivotPoint.transform.Rotate(rotY, 0f, 0f, Space.Self);
        }

        if (Input.GetMouseButton(1))
        {
            float rotX = Input.GetAxis("Mouse X") * _mouseSpeed / 20;
            float rotY = Input.GetAxis("Mouse Y") * _mouseSpeed / 20;

            _modelViewer.PivotPoint.transform.Translate(0f, rotY, 0f, Space.World);
            _modelViewer.PivotPoint.transform.Translate(rotX, 0f, 0f, Space.World);
        }

        if (Input.GetAxis("Mouse ScrollWheel") != 0f)
        {
            _modelViewer.PivotPoint.transform.Translate(0, 0, -Input.GetAxis("Mouse ScrollWheel") * _mouseSpeed, Space.World);
        }
    }
}
