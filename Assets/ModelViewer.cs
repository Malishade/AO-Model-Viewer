using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelViewer : MonoBehaviour
{
    public Camera Camera;
    public GameObject PivotPoint;
    public RDBLoader RdbLoader;
    public GameObject CurrentModel;
    [SerializeField] private float _offset = 1f;

    public void UpdateModel(List<GameObject> meshes)
    {
        DestroyOldModel();
        SetupNewModel(meshes);
        UpdateCameraPosition();
    }

    private void SetupNewModel(List<GameObject> meshes)
    {
        foreach (var mesh in meshes)
        {
            mesh.transform.SetParent(CurrentModel.transform);
        }

        UpdatePivotPosition();

        CurrentModel.transform.SetParent(PivotPoint.transform);
        CurrentModel.transform.Rotate(Vector3.up, 180);
    }

    private void DestroyOldModel()
    {
        if (CurrentModel != null)
            DestroyImmediate(CurrentModel);
      
        CurrentModel = new GameObject();
    }

    private void UpdatePivotPosition()
    {
        PivotPoint.transform.position = CurrentModel.transform.GetMeshBounds().center;
    }

    private void UpdateCameraPosition()
    {
        Bounds bounds = CurrentModel.transform.GetMeshBounds();

        float cameraDistance = _offset;
        Vector3 objectSizes = bounds.max - bounds.min;
        float objectSize = Mathf.Max(objectSizes.x, objectSizes.y, objectSizes.z);
        float cameraView = 2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * Camera.fieldOfView);
        float distance = cameraDistance * objectSize / cameraView;
        distance += 0.5f * objectSize;
        Camera.transform.position = bounds.center - distance * Camera.transform.forward;
    }
}
