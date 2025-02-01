using Apt.Unity.Projection;
using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static AODB.Common.DbClasses.RDBMesh_t;
using Material = UnityEngine.Material;

public class ModelViewer : MonoBehaviour
{
    public Camera MainCamera;
    public PivotController PivotController;
    public ModelPreview ModelPreview;
    [SerializeField] private ScriptableRendererMaterial _renderMaterials;
    [SerializeField] public Material SkeletonMaterial;
    [SerializeField] private GameObject _texturePlanePreset;
    [SerializeField] private ProjectionPlane _projectionPlane;
    [SerializeField] private int _targetFrameRate = 200;

    private static ModelViewer _instance;

    public static ModelViewer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ModelViewer>();
            }

            return _instance;
        }
    }

    void Start()
    {
        ModelPreview = new ModelPreview();
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = _targetFrameRate;
    }

    public void InitUpdateRdbTexture(Material material)
    {
        if (material == null)
            return;

        var mesh = Instantiate(_texturePlanePreset);

        var meshRenderer = mesh.GetComponent<Renderer>();
        var texture = material.GetTexture("_MainTex");

        meshRenderer.material.SetTexture("_MainTex", texture);

        var adjustedScale = mesh.transform.localScale;
        adjustedScale.x = texture.width;
        adjustedScale.z = texture.height;

        mesh.transform.localScale = adjustedScale;

        ModelPreview.DiffuseMaterials = new Dictionary<Renderer, Material> { { meshRenderer, meshRenderer.material } };

        UpdateMesh(mesh);

    }

    public void UpdateMesh(GameObject mesh)
    {
        DestroyCurrentModel();

        if (mesh == null)
            return;

        ModelPreview.SetMeshData(mesh);
        SetupNewModel();
        UpdateParents();
        UpdateCameraPosition();
    }

    public void DestroyCurrentModel()
    {
        if (ModelPreview == null || ModelPreview.PivotRoot == null)
            return;

        DestroyImmediate(ModelPreview.PivotRoot);
    }

    private void SetupNewModel()
    {
        ModelPreview.PivotRoot = new GameObject("PivotRoot");
        ModelPreview.RecalculateBounds();
        ModelPreview.PivotRoot.transform.position = ModelPreview.Bounds.center;
        ModelPreview.GameObjectRoot.transform.SetParent(ModelPreview.PivotRoot.transform);
        ModelPreview.PivotRoot.transform.position = Vector3.zero;
    }

    private void UpdateParents()
    {
        PivotController.transform.position = Vector3.zero;
        PivotController.transform.rotation = Quaternion.identity;
        ModelPreview.PivotRoot.transform.SetParent(PivotController.transform);
        ModelPreview.PivotRoot.transform.Rotate(Vector3.up, 180);

    }

    public void UpdateModelViewer(Vector3 aspectRatioVector)
    {
        UpdateProjectionPlane(aspectRatioVector);
        UpdateCameraPosition();
    }

    public void UpdateProjectionPlane(Vector3 viewAspect)
    {
        var projPlaneSize = _projectionPlane.Size / 2;
        viewAspect = new Vector3(viewAspect.x * projPlaneSize.x, -viewAspect.y * projPlaneSize.y, 0);
        var baseCamPos = new Vector3(-projPlaneSize.x, projPlaneSize.y, 0);

        PivotController.RenderCamera.transform.localPosition = baseCamPos + viewAspect;
        _projectionPlane.transform.position = PivotController.RenderCamera.transform.localPosition * -1;
    }

    public void UpdateCameraPosition()
    {
        if (ModelPreview.GameObjectRoot == null)
            return;

        ModelPreview.RecalculateBounds();
        ModelPreview.PivotRoot.transform.localScale = ModelPreview.PivotRoot.transform.localScale * _projectionPlane.BottomLeft.x / ModelPreview.Bounds.min.x;
        ModelPreview.RecalculateBounds();

        if (ModelPreview.Bounds.max.y > _projectionPlane.TopLeft.y)
        {
            ModelPreview.PivotRoot.transform.localScale = ModelPreview.PivotRoot.transform.localScale * _projectionPlane.TopLeft.y / ModelPreview.Bounds.max.y;
            ModelPreview.RecalculateBounds();
        }

        PivotController.UpdateData(ModelPreview.Bounds);
    }

    public void UpdateMaterial(MaterialTypeId index)
    {
        var currMat = _renderMaterials.RendererMaterials.First(x => x.Index == index).Material;

        if (index == MaterialTypeId.Color)
        {   
            foreach (var mat in ModelPreview.DiffuseMaterials)
            {
                mat.Key.material = mat.Value;
            }
        }
        else if (index == MaterialTypeId.Unlit)
        {
            foreach (var s in ModelPreview.DiffuseMaterials)
            {
                s.Key.material = currMat;
                s.Key.material.SetTexture("_MainTex", s.Value.GetTexture("_MainTex"));
            }
        }
        else
        {
            foreach (var mat in ModelPreview.DiffuseMaterials)
            {
                mat.Key.material = currMat;
            }
        }
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

public class ModelPreview
{
    public Dictionary<Renderer, Material> DiffuseMaterials;
    public GameObject PivotRoot;
    public GameObject GameObjectRoot;
    public Animation Animation;
    public Bounds Bounds;
    public int VerticesCount;
    public int TrianglesCount;

    public void PlayAnimation(string name)
    {
        Animation.Play(name.Replace(".ani", ""));
    }

    public void RecalculateBounds()
    {
        var root = GameObjectRoot.transform.GetComponent<Renderer>();

        if (root != null)
        {
            Bounds = root.bounds;
            return;
        }

        Renderer[] renderers = GameObjectRoot.transform.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;

        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        Bounds = bounds;
    }

    public void SetMeshData(GameObject gameObject)
    {
        Animation = gameObject.GetComponentInChildren<Animation>();
        DiffuseMaterials = new Dictionary<Renderer, Material>();
        GameObjectRoot = gameObject;
        VerticesCount = 0;
        TrianglesCount = 0;

        foreach (UnityEngine.Transform child in gameObject.transform)
        {
            ProcessTransformRecursively(child);
        }
    }

    private void ProcessTransformRecursively(UnityEngine.Transform transform)
    {

        var meshFilter = transform.GetComponent<MeshFilter>();

        if (meshFilter != null)
        {
            ProcessMesh(transform, meshFilter.mesh);
        }

        var skinnedMeshRenderer = transform.GetComponent<SkinnedMeshRenderer>();

        if (skinnedMeshRenderer != null)
        {
            ProcessMesh(transform, skinnedMeshRenderer.sharedMesh);
        }

        foreach (UnityEngine.Transform child in transform)
        {
            ProcessTransformRecursively(child);
        }
    }

    private void ProcessMesh(UnityEngine.Transform transform, Mesh mesh)
    {
        mesh.RecalculateBounds();
        VerticesCount += mesh.vertexCount;
        TrianglesCount += mesh.triangles.Length / 3;

        var renderer = transform.GetComponent<Renderer>();

        if (renderer != null)
        {
            DiffuseMaterials.Add(renderer, renderer.material);
        }
    }
}