using AODB.Common.RDBObjects;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;
using Assimp;
using System.Text.RegularExpressions;
using AODB.Export;
using System.Threading.Tasks;

internal class CirLoader : ModelLoader
{
    public void ExportMesh(int meshId, string path)
    {
        //  Task.Run(() =>
        //  {
        RDBCatMesh catMesh = RDBLoader.Instance.RdbController.Get<RDBCatMesh>(meshId);

        if (catMesh == null)
            return;

        Scene scene;
        List<ResourceEntry> animResources = RDBLoader.Instance.GetAnimIdsForCir(meshId).Select(x => new ResourceEntry(x.Key, x.Value, ResourceTypeId.Anim)).ToList();

        if (animResources.Count > 0)
        {
            animResources = Utils.OrderByIdleness(animResources);
            var animData = animResources.Select(x => new AnimData { CatAnim = RDBLoader.Instance.RdbController.Get<CATAnim>(x.Id), Name = x.Name.Replace(".ani", "") }).ToList();
            scene = CirConvert.ToAssimpScene(catMesh, animData);
        }
        else
        {
            scene = CirConvert.ToAssimpScene(catMesh);
        }

        foreach (Assimp.Material mat in scene.Materials)
        {
            if (mat.HasNonTextureProperty("DiffuseId"))
            {
                RDBLoader.Instance.ExportMeshTexture(mat.GetNonTextureProperty("DiffuseId").GetIntegerValue(), path, out string diffuseName);

                TextureSlot diffuse = new TextureSlot
                {
                    FilePath = diffuseName,
                    TextureType = TextureType.Diffuse,
                    TextureIndex = 0
                };

                RDBLoader.AddMaterialTexture(mat, ref diffuse, false);
            }

            if (mat.HasNonTextureProperty("EmissionId"))
            {
                RDBLoader.Instance.ExportMeshTexture(mat.GetNonTextureProperty("EmissionId").GetIntegerValue(), path, out string emissionName);

                TextureSlot emission = new TextureSlot
                {
                    FilePath = emissionName,
                    TextureType = TextureType.Emissive,
                    TextureIndex = 0,
                };

                RDBLoader.AddMaterialTexture(mat, ref emission, false);
            }
        }

        //Scale x100 and flip on x axis for correct transforms
        foreach (var s in scene.RootNode.Children)
            s.Transform = new Assimp.Matrix4x4(
            -100f, 0, 0, 0,
            0, 100f, 0, 0,
            0, 0, 100f, 0,
            0, 0, 0, 1);

        new AssimpContext().ExportFile(scene, path, "fbx");
        //    });

    }

    public GameObject CreateCirMesh(int meshId, out List<ResourceEntry> animResources)
    {
        animResources = RDBLoader.Instance.GetAnimIdsForCir(meshId).Select(x => new ResourceEntry(x.Key, x.Value, ResourceTypeId.Anim)).ToList();
        var animData = new List<AnimData>();
        var catMesh = RDBLoader.Instance.RdbController.Get<RDBCatMesh>(meshId);

        if (catMesh == null)
        {
            Debug.Log("CatMesh parse failed");
            return new GameObject();
        }

        if (animResources.Count > 0)
        {
            animResources = Utils.OrderByIdleness(animResources);
            animData = animResources.Select(x => new AnimData { CatAnim = RDBLoader.Instance.RdbController.Get<CATAnim>(x.Id), Name = x.Name.Replace(".ani", "") }).ToList();
            var scene = CirConvert.ToAssimpScene(catMesh, animData);

            
            CirLoader cirToUnity = new CirLoader();
            return cirToUnity.MakeMesh(scene, catMesh, animData);
        }
        else
        {
            var scene = CirConvert.ToAssimpScene(catMesh, null);
            CirLoader cirToUnity = new CirLoader();

            return cirToUnity.CreateMeshNode(scene, scene.RootNode, null);
        }
    }

    public GameObject MakeMesh(Scene scene, RDBCatMesh catMesh, List<AnimData> animData)
    {
        var bones = MakeSkeleton(scene);
        var go = SkinCharacter(catMesh, scene, bones);

        foreach (var anim in animData)
            AddAnimationData(bones, anim);

        return go;
    }

    private Transform[] MakeSkeleton(Scene scene)
    {
        Dictionary<string, Transform> boneTransforms = new Dictionary<string, Transform>();

        foreach (var mesh in scene.Meshes)
        {
            foreach (var bone in mesh.Bones)
            {
                if (boneTransforms.ContainsKey(bone.Name))
                    continue;

                var boneTransform = new GameObject(bone.Name).transform;

                if (!bone.Name.Contains("Bone_1_") && !bone.Name.Contains("Bone_0_") && !bone.Name.Contains("Dummy"))
                {
                    boneTransform.AddComponent<DrawBone>();
                }

                boneTransforms.Add(bone.Name, boneTransform);
            }
        }

        List<Node> boneNodes = new List<Node>();

        foreach (var boneTransform in boneTransforms)
        {
            var node = FindBoneNode(scene.RootNode, boneTransform.Key);

            if (node != null)
                boneNodes.Add(FindBoneNode(scene.RootNode, boneTransform.Key));
        }

        foreach (var boneNode in boneNodes)
        {
            BindBone(boneNode, boneTransforms);
        }

        var bones = boneTransforms.Values.OrderBy(x => int.Parse(Regex.Match(x.name, @"\d+").Value)).ToArray();

        return bones;
    }

    private Node FindBoneNode(Node node, string key)
    {
        foreach (var child in node.Children)
        {
            if (child.Name == key)
                return child;

            if (child.HasChildren)
                return FindBoneNode(child, key);
        }

        return null;
    }

    private void BindBone(Node boneNode, Dictionary<string, Transform> boneTransforms)
    {
        foreach (var childBoneNode in boneNode.Children)
        {
            boneTransforms[childBoneNode.Name].parent = boneTransforms[boneNode.Name];

            GetGlobalTransform(childBoneNode).Decompose(out _, out var rot, out var pos);
            boneTransforms[childBoneNode.Name].position = new Vector3(pos.X, pos.Y, pos.Z);
            boneTransforms[childBoneNode.Name].rotation = new UnityEngine.Quaternion(rot.X, rot.Y, rot.Z, rot.W);

            if (!childBoneNode.HasChildren)
                continue;

            BindBone(childBoneNode, boneTransforms);
        }
    }

    private Assimp.Matrix4x4 GetGlobalTransform(Node node)
    {
        Assimp.Matrix4x4 transform = node.Transform;
        while (node.Parent != null)
        {
            node = node.Parent;
            transform = transform * node.Transform;
        }
        return transform;
    }

    private GameObject SkinCharacter(RDBCatMesh rdbCatMesh, Scene scene, Transform[] bones)
    {
        GameObject catMeshObj = new GameObject($"CatMesh()");
       
        bones[0].parent = catMeshObj.transform;
         
        foreach (RDBCatMesh.MeshGroup meshGroup in rdbCatMesh.MeshGroups)
        {
            GameObject meshGroupObj = new GameObject(meshGroup.Name);
            meshGroupObj.transform.parent = catMeshObj.transform;

            foreach (RDBCatMesh.Mesh mesh in meshGroup.Meshes)
            {
                GameObject faceObj = new GameObject("Mesh");
                faceObj.transform.parent = meshGroupObj.transform;

                var renderer = faceObj.AddComponent<SkinnedMeshRenderer>();
                renderer.bones = bones;
                renderer.quality = SkinQuality.Bone2;

                renderer.material = RDBLoader.LoadMaterial(scene.Materials[mesh.MaterialId]);
                renderer.material.SetFloat("_Glossiness", 0f);

                UnityEngine.Mesh meshObj = new UnityEngine.Mesh()
                {
                    vertices = mesh.Vertices.Select(x => GetVertexSkeletonPos(x, bones)).ToArray(),
                    triangles = mesh.Triangles,
                    uv = mesh.Vertices.Select(x => new Vector2(x.Uvs.X, x.Uvs.Y)).ToArray(),
                    boneWeights = mesh.Vertices.Select(x => new BoneWeight()
                    {
                        boneIndex0 = x.Joint1,
                        boneIndex1 = x.Joint2,
                        weight0 = x.Joint1Weight,
                        weight1 = 1 - x.Joint1Weight
                    }).ToArray(),
                    bindposes = bones.Select(x => x.worldToLocalMatrix * catMeshObj.transform.localToWorldMatrix).ToArray(),
                    normals = mesh.Vertices.Select(x => x.Normal.ToUnity()).ToArray(),
                };

                meshObj.RecalculateNormals();
                meshObj.RecalculateTangents();

                renderer.sharedMesh = meshObj;
            }
        }

        return catMeshObj;
    }

    private void AddAnimationData(Transform[] bones, AnimData animData)
    {
        Dictionary<string, List<QuaternionKey>> rotKeys = new Dictionary<string, List<QuaternionKey>>();
        Dictionary<string, List<VectorKey>> transKeys = new Dictionary<string, List<VectorKey>>();

        foreach (BoneData boneData in animData.CatAnim.Animation.BoneData)
        {
            var qKeys = new List<QuaternionKey>();

            foreach (var rot in boneData.RotationKeys)
                qKeys.Add(new QuaternionKey
                {
                    Time = rot.Time / 1000f,
                    Value = new Assimp.Quaternion(rot.Rotation.W, rot.Rotation.X, rot.Rotation.Y, rot.Rotation.Z)
                });

            rotKeys.Add(bones[boneData.BoneId].name, qKeys);

            var tKeys = new List<VectorKey>();

            foreach (var tra in boneData.TranslationKeys)
                tKeys.Add(new VectorKey
                {
                    Time = tra.Time / 1000f,
                    Value = new Assimp.Vector3D(tra.Position.X, tra.Position.Y, tra.Position.Z)
                });

            transKeys.Add(bones[boneData.BoneId].name, tKeys);
        }

        AddAnimationData(bones[0].gameObject, animData.Name, rotKeys, transKeys);
    }

    private Vector3 GetVertexSkeletonPos(RDBCatMesh.Vertex vertex, Transform[] bones)
    {
        Vector3 relToPos1 = new Vector3(vertex.RelToJoint1.X, vertex.RelToJoint1.Y, vertex.RelToJoint1.Z);
        Vector3 relToPos2 = new Vector3(vertex.RelToJoint2.X, vertex.RelToJoint2.Y, vertex.RelToJoint2.Z);

        return Vector3.Lerp(bones[vertex.Joint2].position + bones[vertex.Joint2].transform.rotation * relToPos2, bones[vertex.Joint1].transform.position + bones[vertex.Joint1].transform.rotation * relToPos1, vertex.Joint1Weight);
    }
}