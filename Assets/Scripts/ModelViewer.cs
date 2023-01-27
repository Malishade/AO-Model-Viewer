using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelViewer : MonoBehaviour
{
    public Camera MainCamera;
    public ScriptableRendererMaterial RenderMaterials;
    public GameObject TexturePlanePreset;
    public PivotController PivotController;

    [HideInInspector] public GameObject CurrentModelRoot;
    [HideInInspector] public List<MeshRenderer> CurrentModelMeshes;
   
    [SerializeField] private float _offset = 1f;
    [SerializeField] private int _targetFrameRate = 200;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = _targetFrameRate;
    }

    public void InitUpdateTexture(Material material)
    {
        var mesh = Instantiate(TexturePlanePreset);
        var meshRenderer = mesh.GetComponent<MeshRenderer>();
        var texture = material.GetTexture("_MainTex");

        meshRenderer.material.SetTexture("_MainTex", texture);

        var adjustedScale = mesh.transform.localScale;
        adjustedScale.x = texture.width;
        adjustedScale.z = texture.height;

        mesh.transform.localScale = adjustedScale;

        var meshes = new List<GameObject> { mesh.gameObject };

        CurrentModelMeshes = new List<MeshRenderer> { meshRenderer };

        UpdateModelViewer(meshes);

        float orthoSize = texture.height < texture.width ? 2.8125f * texture.width : 5f * texture.height;
        PivotController.RenderCamera.orthographicSize = orthoSize;
    }

    public void InitUpdateRdbMesh(List<GameObject> meshes)
    {
        CurrentModelMeshes = meshes.GetMeshRenderers();
        UpdateModelViewer(meshes);
    }

    private void UpdateModelViewer(List<GameObject> meshes)
    {
        Bounds bounds = CurrentModelMeshes.GetMeshBounds();
        
        DestroyCurrentModel();
        SetupNewModel(meshes, bounds);
        UpdateParents(bounds);
        UpdateCameraPosition(bounds);
    }

    public void DestroyCurrentModel()
    {
        if (CurrentModelRoot != null)
            DestroyImmediate(CurrentModelRoot);
    }

    private void SetupNewModel(List<GameObject> meshes, Bounds bounds)
    {
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
        float cameraView = 2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * MainCamera.fieldOfView);
        float distance = cameraDistance * objectSize / cameraView;
        distance += 0.5f * objectSize;
        MainCamera.transform.position = bounds.center - distance * MainCamera.transform.forward;
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
