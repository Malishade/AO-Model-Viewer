using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DrawBone : MonoBehaviour
{
    private LineRenderer lineRenderer;
  
    public void Start()
    {
        lineRenderer = transform.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
        lineRenderer.material = ModelViewer.Instance.SkeletonMaterial;
    }

    private void Update()
    {

        if (transform.parent)
            lineRenderer.SetPositions(new Vector3[] { transform.position, transform.parent.position });
    }
}
