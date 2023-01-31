using Apt.Unity.Projection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        CurrentModelData.MeshRenderers = new Dictionary<MeshRenderer, Texture> { { meshRenderer, texture } };

        UpdateModelViewer();
    }

    public void InitUpdateRdbMesh(GameObject meshes)
    {
        CurrentModelData.SetMeshData(meshes);
        UpdateModelViewer();
    }

    private void UpdateModelViewer()
    {
        CurrentModelData.RecalculateBounds();

        DestroyCurrentModel();
        SetupNewModel();
        UpdateParents();
        UpdateCameraPosition();
    }

    public void DestroyCurrentModel()
    {
        if (CurrentModelData.Root != null)
            DestroyImmediate(CurrentModelData.Root);
    }

    private void SetupNewModel()
    {
        CurrentModelData.Root = new GameObject();
        CurrentModelData.Root.transform.position = CurrentModelData.Bounds.center;

        foreach (var mesh in CurrentModelData.MeshRenderers.Keys)
            mesh.transform.SetParent(CurrentModelData.Root.transform);

        CurrentModelData.Root.transform.position = Vector3.zero;
    }

    private void UpdateParents()
    {
        PivotController.transform.position = Vector3.zero;
        PivotController.transform.rotation = Quaternion.identity;
        CurrentModelData.Root.transform.SetParent(PivotController.transform);
        CurrentModelData.Root.transform.Rotate(Vector3.up, 180);
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
        if (CurrentModelData.Root == null)
            return;

        CurrentModelData.Root.transform.localScale = CurrentModelData.Root.transform.localScale * _projectionPlane.BottomLeft.x / CurrentModelData.Bounds.min.x;
        CurrentModelData.RecalculateBounds();

        if (CurrentModelData.Bounds.max.y > _projectionPlane.TopLeft.y)
        {
            CurrentModelData.Root.transform.localScale = CurrentModelData.Root.transform.localScale * _projectionPlane.TopLeft.y / CurrentModelData.Bounds.max.y;
            CurrentModelData.RecalculateBounds();
        }

        PivotController.UpdateData(CurrentModelData.Bounds);
    }

    public void UpdateMaterial(MaterialTypeId index)
    {
        var currMat = _renderMaterials.RendererMaterials.First(x => x.Index == index).Material;

        foreach (var s in CurrentModelData.MeshRenderers)
        {
            s.Key.material = currMat;
            s.Key.material.SetTexture("_MainTex", s.Value);
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
    public Dictionary<MeshRenderer,Texture> MeshRenderers;
    public GameObject Root;
    public Bounds Bounds;
    public int VerticesCount;
    public int TrianglesCount;

    public void RecalculateBounds()
    {
        Bounds = new Bounds();

        foreach (MeshRenderer child in MeshRenderers.Keys)
        {
            Bounds.Encapsulate(child.bounds);
        }
    }

    //public static List<MeshRenderer> GetMeshRenderers(this Dictionary<MeshRenderer, Texture> gameObjects)
    //{
    //    List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

    //    foreach (MeshRenderer child in gameObjects.Keys)
    //    {
    //        meshRenderers.Add(child);
    //    }

    //    return meshRenderers;
    //}

    //public static void GetMeshData(this List<MeshRenderer> meshes, out MeshData meshData)
    //{


    //    meshData = new ();
    //    meshData.MeshRendererData = new();

    //    foreach (MeshRenderer mesh in meshes)
    //    {
    //        var texture = mesh.material.GetTexture("_MainTex");

    //        if (texture != null)
    //            meshData.MeshRendererData.Add(mesh, texture);

    //        var meshFilter = mesh.gameObject.GetComponent<MeshFilter>();
    //        meshData.VerticesCount += meshFilter.sharedMesh.vertexCount;
    //        meshData.TrianglesCount += meshFilter.sharedMesh.triangles.Length/3;
    //    }
    //}
    public void SetMeshData(GameObject gameObject)
    {
        Dictionary<MeshRenderer, Texture> meshRenderers = new Dictionary<MeshRenderer, Texture>();

        foreach (MeshRenderer child in gameObject.transform.GetComponentsInChildren<MeshRenderer>())
        {
            var meshFilter = child.GetComponent<MeshFilter>();

            if (meshFilter == null)
                continue;

            var texture = child.material.GetTexture("_MainTex");
            VerticesCount += meshFilter.sharedMesh.vertexCount;
            TrianglesCount += meshFilter.sharedMesh.triangles.Length / 3;
            meshRenderers.Add(child, texture);
        }

        MeshRenderers = meshRenderers;
    }
}
