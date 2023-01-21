using UnityEngine;

public class ModelRotatorController : MonoBehaviour
{
    [SerializeField] private float _mouseSpeed = 10f; 
    [SerializeField] private float _slerpSpeed = 100f;
    [SerializeField] private ModelViewer _modelViewer;

    void Update()
    {
        if (_modelViewer.CurrentModel == null)
            return;

        if (!Input.GetMouseButton(0))
            return;

        float rotX = Input.GetAxis("Mouse X") * _mouseSpeed;
        float rotY = Input.GetAxis("Mouse Y") * _mouseSpeed;

        Vector3 right = Vector3.Cross(_modelViewer.Camera.transform.up, _modelViewer.CurrentModel.transform.position - _modelViewer.Camera.transform.position);
        Vector3 up = Vector3.Cross(_modelViewer.CurrentModel.transform.position - _modelViewer.Camera.transform.position, right);

        _modelViewer.PivotPoint.transform.rotation =
            Quaternion.Slerp(_modelViewer.PivotPoint.transform.rotation, Quaternion.AngleAxis(-rotX, up) * _modelViewer.PivotPoint.transform.rotation, Time.deltaTime * _slerpSpeed);

        _modelViewer.PivotPoint.transform.rotation =
            Quaternion.Slerp(_modelViewer.PivotPoint.transform.rotation, Quaternion.AngleAxis(rotY, right) * _modelViewer.PivotPoint.transform.rotation, Time.deltaTime * _slerpSpeed);
    }
}
