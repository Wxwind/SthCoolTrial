using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    private Vector3Int m_coord;
    public Vector3Int coord
    {
        get => m_coord;
    }
    
    [ReadOnly]
    public Mesh mesh;
    
    private MeshFilter m_meshFliter;
    private MeshCollider m_meshCollider;
    private MeshRenderer m_meshRenderer;

    //初始化组件和属性
    public void SetUp(Vector3Int coord,Material chunkMat)
    {
        m_coord = coord;
        m_meshFliter = GetComponent<MeshFilter> ();
        m_meshRenderer = GetComponent<MeshRenderer> ();
        m_meshCollider = GetComponent<MeshCollider> ();

        if (m_meshFliter == null) {
            m_meshFliter = gameObject.AddComponent<MeshFilter> ();
        }

        if (m_meshRenderer == null) {
            m_meshRenderer = gameObject.AddComponent<MeshRenderer> ();
        }

        if (m_meshCollider == null && m_meshCollider) {
            m_meshCollider = gameObject.AddComponent<MeshCollider> ();
        }

        mesh = m_meshFliter.sharedMesh;
        if (mesh == null) {
            mesh = new Mesh ();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m_meshFliter.sharedMesh = mesh;
        }
        m_meshRenderer.material = chunkMat;
    }
}
