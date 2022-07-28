using System;
using System.Collections.Generic;
using System.Linq;
using MarchingCube.Scripts;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

[ExecuteInEditMode]
public class MeshGenerator : MonoBehaviour
{
    public bool isRunInEditorMode;
    
    [Title("Chunk Settings")]
    public Vector3Int numChunks;//chunk在每个轴上的个数
    [Range(2, 30)] public int numPointsPerAxis; //一个chunk内每一个坐标轴上的采样点个数
    public float chunkSize = 20;
    public Material chunkMaterial;
    [ShowInInspector] private const int threadGroupSize = 8; //一个chunk内，外层每个线程组要处理的每个轴上的voxel数
    [ShowInInspector, ReadOnly] private List<Chunk> m_chunks;
    
    [Title("ComputeShader Setting")]
    public DensityGenerator densityGenerator;
    public ComputeShader marchingCubesShader;

    [Title("Voxel Settings")] 
    public float isoLevel;//只有大于该值的voxel顶点才会参与计算(使该voxel顶点处于生最终成的形体内部)
    public Vector3 offset = Vector3.zero;
    
    [Title("Bound Visualization")] 
    public bool IsShowBoundsGizmo;
    [ShowIf("IsShowBoundsGizmo")]
    public Color boundsGizmoColor;

    private const string m_chunkListName = "ChunkManager";
    private GameObject m_chunkManager;
    public bool isSettingsUpdated;

    private ComputeBuffer m_triangleBuffer;
    private ComputeBuffer m_triCountBuffer;
    private ComputeBuffer m_pointsBuffer;
    
    private void Update()
    {
        if (isSettingsUpdated)
        {
            if (isRunInEditorMode && !Application.isPlaying)
            {
                Run();
            }

            isSettingsUpdated = false;
        }
    }

    public void Run()
    {
        InitMaterial();
        CreateBuffers();
        InitChunks();
        foreach (var c in m_chunks)
        {
            UpdateChunkMesh(c);
        }

        //如果在编辑器模式下则立即释放ComputeBuffers
        if (!Application.isPlaying)
        {
            ReleaseBuffers();
        }
    }

    void InitMaterial()
    {
        var totalSize = chunkSize * numChunks.y;
        chunkMaterial.SetFloat("_BoundsY",totalSize);
    }

    void OnValidate()
    {
        isSettingsUpdated = true;
    }
    
    void OnDestroy () {
        if (Application.isPlaying) {
            ReleaseBuffers ();
        }
    }

    void CreateBuffers()
    {
        int numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        int numVoxelsPerAxis = numPointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCounts = numVoxels * 5;
        //如果处于playmode且numPoints大小不变，则不修改CommandBuffer
        //否则重新申请CommandBuffer，并且在playmode下还要释放commandBuffer(编辑器模式下在run函数中立马释放buffer)
        if (Application.isPlaying && (m_pointsBuffer != null && numPoints == m_pointsBuffer.count)) return;
        if (Application.isPlaying)
        {
            ReleaseBuffers();
        }

        m_triangleBuffer = new ComputeBuffer(maxTriangleCounts, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        m_pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
        m_triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
    }

    void ReleaseBuffers()
    {
        if (m_triangleBuffer != null)
        {
            m_triangleBuffer.Release();
            m_pointsBuffer.Release();
            m_triCountBuffer.Release();
        }
    }

    void InitChunks()
    {
        CreateChunkList();
        List<Chunk> oldChunks = new List<Chunk>(FindObjectsOfType<Chunk>());
        //Debug.Log("old chunks count:"+oldChunks.Count);
        
        //在editor模式下chunks不为null，但开始运行后m_chunks丢失引用
        m_chunks = new List<Chunk>();

        //生成Chunks
        for (int x = 0; x < numChunks.x; x++)
        {
            for (int y = 0; y < numChunks.y; y++)
            {
                for (int z = 0; z < numChunks.z; z++)
                {
                    Vector3Int coord = new Vector3Int(x, y, z);
                    var a = oldChunks.FirstOrDefault(chunk => chunk.coord == coord);
                    if (a != null)
                    {
                        m_chunks.Add(a);
                        oldChunks.Remove(a);
                    }
                    else
                    {
                        var newChunk = CreateChunk(coord);
                        m_chunks.Add(newChunk);
                    }
                }
            }
        }

        //清除多余的chunks
        foreach (var c in oldChunks)
        {
            if (c != null)
            {
                DestroyImmediate(c.gameObject);
            }
        }
    }

    void UpdateChunkMesh(Chunk chunk)
    {
        int numVoxelsPerAxis = numPointsPerAxis - 1; //一个chunk内每个坐标轴上有多少voxel
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float) threadGroupSize); //一个chunk内，外层线程组每个轴上的线程组数量,如果采用FloorToInt可能会导致线程处理不到所有的数据
        float voxelSize = chunkSize / numVoxelsPerAxis; //一个chunk内voxel大小
        Vector3 centerPos = CenterFromCoord(chunk.coord);
        Vector3 worldBounds = (Vector3) chunk.coord * chunkSize;

        densityGenerator.Generate(m_pointsBuffer, numPointsPerAxis, chunkSize, centerPos, offset, voxelSize,
            worldBounds);//使用NoiseDensity ComputeShader填充m_pointsBuffer, xyz为坐标,w为isolevel
        m_triangleBuffer.SetCounterValue(0);
        
        // Vector4[] Farray = new Vector4[numPointsPerAxis * numPointsPerAxis * numPointsPerAxis];
        // m_pointsBuffer.GetData(Farray,0,0,numPointsPerAxis * numPointsPerAxis * numPointsPerAxis);
        // foreach (var a in Farray)
        // {
        //     Debug.Log(a);
        // }
        
        marchingCubesShader.SetBuffer(0, "points", m_pointsBuffer);
        marchingCubesShader.SetBuffer(0, "triangles", m_triangleBuffer);
        marchingCubesShader.SetInt("numPointsPerAxis", numPointsPerAxis);
        marchingCubesShader.SetFloat("isoLevel", isoLevel);
        //调用MarchingCubes ComputeShader
        marchingCubesShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        //处理computeShader结果
        ComputeBuffer.CopyCount(m_triangleBuffer, m_triCountBuffer, 0); //获取triangleBuffer中三角形个数并存储到triCountBuffer中
        //读取m_triCountBuffer中的数据
        int[] triCountArray = {0};
        m_triCountBuffer.GetData(triCountArray); 
        int numTriangles = triCountArray[0];
        Triangle[] triangleDataArray = new Triangle[numTriangles];
        m_triangleBuffer.GetData(triangleDataArray, 0, 0, numTriangles);

        Mesh mesh = chunk.mesh;
        mesh.Clear();
        
        var vertices = new Vector3[numTriangles * 3];
        var triangles = new int[numTriangles * 3];
        for (int i = 0; i < numTriangles; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                vertices[i * 3 + j] = triangleDataArray[i][j];
                triangles[i * 3 + j] = i * 3 + j;
            }
        }

        // string o=String.Empty;
        // foreach (var a in triangles)
        // {
        //     o += a;
        // }
        // string ta=String.Empty;
        // foreach (var a in vertices)
        // {
        //     ta += a.ToString()+"\n";
        // }
        //
        // Debug.Log(ta);
        // Debug.Log(o);
        //Debug.Log(triangles.Length);
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        string nors=String.Empty;
        // foreach (var a in mesh.normals)
        // {
        //     nors += a.ToString()+"\n";
        // }
        // Debug.Log(nors);
    }

    struct Triangle
    {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }

    //coord(0,0,0)对应xyz最小处，世界坐标(0,0,0)是整个marchingCubes的中心，因此每个coord的中心应做如下计算
    Vector3 CenterFromCoord(Vector3Int coord)
    {
        Vector3 totalBounds = (Vector3) numChunks * chunkSize;
        //coord顶点中xyz最小的坐标作为基准点再加上该点到中心点的距离
        return -totalBounds / 2 + (Vector3) coord * chunkSize + Vector3.one * chunkSize / 2;
        //return (Vector3) coord * boundsSize;
    }

    Chunk CreateChunk(Vector3Int coord)
    {
        GameObject go = new GameObject($"Chunk ({coord.x},{coord.y},{coord.z})");
        go.transform.parent = m_chunkManager.transform;
        var newChunk = go.AddComponent<Chunk>();
        newChunk.SetUp(coord,chunkMaterial);
        return newChunk;
    }

    void CreateChunkList()
    {
        if (m_chunkManager == null)
        {
            m_chunkManager = GameObject.Find(m_chunkListName)
                ? GameObject.Find(m_chunkListName)
                : new GameObject(m_chunkListName);
        }
    }
    
    void OnDrawGizmos () {
        if (IsShowBoundsGizmo) {
            Gizmos.color = boundsGizmoColor;

            List<Chunk> chunks = this.m_chunks ?? new List<Chunk> (FindObjectsOfType<Chunk> ());
            foreach (var chunk in chunks) {
                Gizmos.color = boundsGizmoColor;
                Gizmos.DrawWireCube (CenterFromCoord(chunk.coord), Vector3.one * chunkSize);
            }
        }
    }
}