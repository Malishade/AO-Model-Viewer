using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelViewer : MonoBehaviour
{
    public Camera Camera;
    public GameObject CurrentModel;
    public PivotController PivotController;
    [SerializeField] private float _offset = 1f;

    public void UpdateModel(List<GameObject> meshes)
    {
        Bounds newBounds = meshes.GetMeshBounds();

        SetupNewModel(meshes, newBounds);
        UpdateParents(newBounds);
        UpdateCameraPosition(newBounds);
    }

    private void SetupNewModel(List<GameObject> meshes, Bounds bounds)
    {
        if (CurrentModel != null)
            DestroyImmediate(CurrentModel);

        CurrentModel = new GameObject();
        CurrentModel.transform.position = bounds.center;

        foreach (var mesh in meshes)
            mesh.transform.SetParent(CurrentModel.transform);
    }

    private void UpdateParents(Bounds bounds)
    {   
        PivotController.UpdateData(bounds);
        CurrentModel.transform.SetParent(PivotController.transform);
        CurrentModel.transform.Rotate(Vector3.up, 180);
    }


    private void UpdateCameraPosition(Bounds bounds)
    {
        float cameraDistance = _offset;
        Vector3 objectSizes = bounds.max - bounds.min;
        float objectSize = Mathf.Max(objectSizes.x, objectSizes.y, objectSizes.z);
        float cameraView = 2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * Camera.fieldOfView);
        float distance = cameraDistance * objectSize / cameraView;
        distance += 0.5f * objectSize;
        Camera.transform.position = bounds.center - distance * Camera.transform.forward;
    }

    void OnApplicationQuit() => RDBLoader.Instance.CloseDatabase();
}
