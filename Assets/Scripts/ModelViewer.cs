using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelViewer : MonoBehaviour
{
    public Camera Camera;
    public ScriptableRendererMaterial RenderMaterials;
    [HideInInspector] public GameObject CurrentModelRoot;
    [HideInInspector] public List<MeshRenderer> CurrentModelMeshes;
    public PivotController PivotController;
    [SerializeField] private float _offset = 1f;
    [SerializeField] private int _targetFrameRate = 144;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = _targetFrameRate;
    }

    public void UpdateModel(List<GameObject> meshes)
    {
        CurrentModelMeshes = meshes.GetMeshRenderers();
        Bounds newBounds = CurrentModelMeshes.GetMeshBounds();

        SetupNewModel(meshes, newBounds);
        UpdateParents(newBounds);
        UpdateCameraPosition(newBounds);
    }

    private void SetupNewModel(List<GameObject> meshes, Bounds bounds)
    {
        if (CurrentModelRoot != null)
            DestroyImmediate(CurrentModelRoot);

        CurrentModelRoot = new GameObject();
        CurrentModelRoot.transform.position = bounds.center;

        foreach (var mesh in meshes)
            mesh.transform.SetParent(CurrentModelRoot.transform);
    }

    private void UpdateParents(Bounds bounds)
    {   
        PivotController.UpdateData(bounds);
        CurrentModelRoot.transform.SetParent(PivotController.transform);
        CurrentModelRoot.transform.Rotate(Vector3.up, 180);
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

[Serializable]
public class RenderMaterials
{
    public Material Diffuse;
    public Material Unlit;
    public Material Wireframe;
    public Material Matcap;
}
