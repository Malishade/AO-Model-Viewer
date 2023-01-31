using Apt.Unity.Projection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Material = UnityEngine.Material;

public class ModelViewer : MonoBehaviour
{
    public Camera MainCamera;
    [SerializeField] private ScriptableRendererMaterial _renderMaterials;
    [SerializeField] private GameObject _texturePlanePreset;
    public PivotController PivotController;

    //[HideInInspector] public GameObject CurrentModelRoot;
    //[HideInInspector] public List<MeshRenderer> CurrentModelMeshes;

    [SerializeField] private ProjectionPlane _projectionPlane;
    [SerializeField] private int _targetFrameRate = 200;

   // private Bounds _currentModelBounds;

    public CurrentModelData CurrentModelData;

    void Start()
    {
        CurrentModelData = new CurrentModelData();
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = _targetFrameRate;
    }

    public void InitUpdateRdbTexture(Material material)
    {
        var mesh = Instantiate(_texturePlanePreset);
        var meshRenderer = mesh.GetComponent<MeshRenderer>();
        var texture = material.GetTexture("_MainTex");

        meshRenderer.material.SetTexture("_MainTex", texture);

        var adjustedScale = mesh.transform.localScale;
        adjustedScale.x = texture.width;
        adjustedScale.z = texture.height;

        mesh.transform.localScale = adjustedScale;

        CurrentModelData.DiffuseMaterials = new Dictionary<MeshRenderer, Material> { { meshRenderer, meshRenderer.material } };

        UpdateModelViewer();
    }

    public void InitUpdateRdbMesh(GameObject meshes)
    {
        CurrentModelData.SetMeshData(meshes);
        UpdateModelViewer();
    }

    private void UpdateModelViewer()
    {
        DestroyCurrentModel();
        SetupNewModel();
        UpdateParents();
        UpdateCameraPosition();
    }

    public void DestroyCurrentModel()
    {
        if (CurrentModelData.PivotRoot != null)
            DestroyImmediate(CurrentModelData.PivotRoot);
    }

    private void SetupNewModel()
    {
        CurrentModelData.PivotRoot = new GameObject();
        CurrentModelData.RecalculateBounds();
        CurrentModelData.PivotRoot.transform.position = CurrentModelData.Bounds.center;

        CurrentModelData.GameObjectRoot.transform.SetParent(CurrentModelData.PivotRoot.transform);

        CurrentModelData.PivotRoot.transform.position = Vector3.zero;
    }

    private void UpdateParents()
    {
        PivotController.transform.position = Vector3.zero;
        PivotController.transform.rotation = Quaternion.identity;
        CurrentModelData.PivotRoot.transform.SetParent(PivotController.transform);
        CurrentModelData.PivotRoot.transform.Rotate(Vector3.up, 180);
        CurrentModelData.PivotRoot.transform.Rotate(Vector3.right, 90);

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
        if (CurrentModelData.GameObjectRoot == null)
            return;

        CurrentModelData.RecalculateBounds();
        CurrentModelData.PivotRoot.transform.localScale = CurrentModelData.PivotRoot.transform.localScale * _projectionPlane.BottomLeft.x / CurrentModelData.Bounds.min.x;
        CurrentModelData.RecalculateBounds();

        if (CurrentModelData.Bounds.max.y > _projectionPlane.TopLeft.y)
        {
            CurrentModelData.PivotRoot.transform.localScale = CurrentModelData.PivotRoot.transform.localScale * _projectionPlane.TopLeft.y / CurrentModelData.Bounds.max.y;
            CurrentModelData.RecalculateBounds();
        }

        PivotController.UpdateData(CurrentModelData.Bounds);
    }

    public void UpdateMaterial(MaterialTypeId index)
    {
        var currMat = _renderMaterials.RendererMaterials.First(x => x.Index == index).Material;

        if (index == MaterialTypeId.Color)
        {
            foreach (var s in CurrentModelData.DiffuseMaterials)
            {
                s.Key.material = s.Value;
            }
        }
        else if (index == MaterialTypeId.Unlit)
        {
            foreach (var s in CurrentModelData.DiffuseMaterials)
            {
                s.Key.material = currMat;
                s.Key.material.SetTexture("_MainTex", s.Value.GetTexture("_MainTex"));
            }
        }
        else
        {
            foreach (var s in CurrentModelData.DiffuseMaterials)
            {
                s.Key.material = currMat;
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

public class CurrentModelData
{
    public Dictionary<MeshRenderer, Material> DiffuseMaterials;
    public GameObject PivotRoot;
    public GameObject GameObjectRoot;

    public Bounds Bounds;
    public int VerticesCount;
    public int TrianglesCount;

    public void RecalculateBounds()
    {
        Renderer[] rr = GameObjectRoot.transform.GetComponentsInChildren<Renderer>();
        Bounds b = rr[0].bounds;
        foreach (Renderer r in rr) { b.Encapsulate(r.bounds); }
        Bounds = b;
    }

    public void SetMeshData(GameObject gameObject)
    {
        DiffuseMaterials = new Dictionary<MeshRenderer, Material>();
        GameObjectRoot = gameObject;

        foreach (Transform child in gameObject.transform.GetComponentsInChildren<Transform>())
        {
            var meshFilter = child.GetComponent<MeshFilter>();

            if (meshFilter == null)
                continue;
            meshFilter.mesh.RecalculateBounds();
            VerticesCount += meshFilter.sharedMesh.vertexCount;
            TrianglesCount += meshFilter.sharedMesh.triangles.Length / 3;

            var meshRenderer = child.GetComponent<MeshRenderer>();
            DiffuseMaterials.Add(meshRenderer, meshRenderer.material);
        }

        Debug.Log(DiffuseMaterials.Count());
    }
}
