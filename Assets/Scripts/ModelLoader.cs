using AODB.Common.RDBObjects;
using Assimp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using static AODB.Common.DbClasses.RDBMesh_t.FAFAnim_t;

internal class ModelLoader
{
    public GameObject CreateMeshNode(Scene scene, Node node, Dictionary<int, UVKey[]> uvAnims)
    {
        var root = CreateRootMeshObject(scene, node);

        if (node.HasMeshes)
        {
            foreach (var meshIdx in node.MeshIndices)
            {
                var aMesh = scene.Meshes[meshIdx];
                var mesh = new UnityEngine.Mesh()
                {
                    vertices = aMesh.Vertices.ToUnityArray(),
                    triangles = aMesh.GetIndices(),
                    normals = aMesh.Normals.ToUnityArray(),
                    uv = aMesh.TextureCoordinateChannels[0].ToUnity2DArray(),
                };
                var child = CreateMeshObject(scene, root.transform, mesh, meshIdx, out Assimp.Material material);
                child.AddComponent<MeshRenderer>().material = RDBLoader.LoadMaterial(material);

                if (uvAnims != null && uvAnims.TryGetValue(meshIdx, out UVKey[] uvAnim) && uvAnim != null)
                {
                    child.AddComponent<UVAnimation>().Init(uvAnim);
                }
            }
        }

        foreach (Node child in node.Children)
        {
            GameObject childObj = CreateMeshNode(scene, child, uvAnims);
            childObj.transform.SetParent(root.transform, false);
        }

        return root;
    }
    private GameObject CreateRootMeshObject(Scene scene, Node node)
    {
        GameObject nodeObj = new GameObject(node.Name);
        node.Transform.Decompose(out Vector3D scale, out Assimp.Quaternion rotation, out Vector3D position);
        nodeObj.transform.localScale = new Vector3(scale.X, scale.Y, scale.Z);
        nodeObj.transform.rotation = new UnityEngine.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
        nodeObj.transform.position = new Vector3(position.X, position.Y, position.Z);

        return nodeObj;
    }

    private GameObject CreateMeshObject(Scene scene, Transform parent, UnityEngine.Mesh mesh, int idx, out Assimp.Material material)
    {
        GameObject submeshObj = new GameObject("MeshPart");
        MeshFilter meshFilter = submeshObj.AddComponent<MeshFilter>();
        material = scene.Materials[scene.Meshes[idx].MaterialIndex];
        submeshObj.transform.SetParent(parent, false);
        meshFilter.mesh = mesh;
        return submeshObj;
    }


    public void AddAnimationData(GameObject rootNode, string animName, Dictionary<string, List<QuaternionKey>> rotKeys, Dictionary<string, List<VectorKey>> transKeys)
    {
        AnimationClip animClip = new AnimationClip();
        animClip.legacy = true;
        animClip.name = animName;
        animClip.wrapMode = WrapMode.Loop;

        foreach (var transKey in transKeys)
        {
            var match = FindChildPath(rootNode.transform, transKey.Key);

            var curve = new AnimationCurve(transKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.X)).ToArray());
            animClip.SetCurve(match, typeof(Transform), "localPosition.x", new AnimationCurve(transKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.X)).ToArray()));
            animClip.SetCurve(match, typeof(Transform), "localPosition.y", new AnimationCurve(transKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.Y)).ToArray()));
            animClip.SetCurve(match, typeof(Transform), "localPosition.z", new AnimationCurve(transKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.Z)).ToArray()));
        }

        foreach (var rotKey in rotKeys)
        {

            var match = FindChildPath(rootNode.transform, rotKey.Key);

            var rotX = new AnimationCurve(rotKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.X)).ToArray());
            var rotY = new AnimationCurve(rotKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.Y)).ToArray());
            var rotZ = new AnimationCurve(rotKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.Z)).ToArray());
            var rotW = new AnimationCurve(rotKey.Value.Select(x => new Keyframe((float)x.Time, x.Value.W)).ToArray());

            animClip.SetCurve(match, typeof(Transform), "localRotation.x", rotX);
            animClip.SetCurve(match, typeof(Transform), "localRotation.y", rotY);
            animClip.SetCurve(match, typeof(Transform), "localRotation.z", rotZ);
            animClip.SetCurve(match, typeof(Transform), "localRotation.w", rotW);

        }

        if (!rootNode.TryGetComponent<UnityEngine.Animation>(out var anim))
            anim = rootNode.AddComponent<UnityEngine.Animation>();

        animClip.EnsureQuaternionContinuity();
        anim.clip = animClip;
        anim.AddClip(animClip, animClip.name);

        if (anim.GetClipCount() == 1)
            anim.Play();
    }


    private string FindChildPath(Transform parent, string childName)
    {
        if (parent.name == childName)
            return "";

        Transform target = FindChildRecursive(parent, childName);

        if (target != null)
        {
            return GetPathToRoot(target, parent);
        }
        return null;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    private string GetPathToRoot(Transform child, Transform root)
    {
        string path = child.name;
        Transform current = child;

        while (current.parent != null && current.parent != root)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}