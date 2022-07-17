using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace MarchingCube.Scripts
{

    public class DensityGenerator : MonoBehaviour
    {
        [Header("Noise FBM Settings")] public int seed;
        [MinValue(1)]
        public int octaves = 4;//分型个数
        public float lacunarity = 2;//频率变化系数
        public float gain = .5f;//幅度变化系数
        public float noiseFrequency = 5; //frequency的缩放系数=noiseFrequency/100
        public float noiseWeight = 1;
        //public bool closeEdges;
        public float isoLevelOffset = 1;//对每个voxel顶点的isoLevel减去偏移，这也意味着值越大，那么形体会包住更多的顶点
        //public float weightMultiplier = 1;

        // public float hardFloorHeight;
        // public float hardFloorWeight;
        //
        // public Vector4 shaderParams;

        [ShowInInspector] const int threadGroupSize = 8;

        public ComputeShader NoiseDensityShader;
        private List<ComputeBuffer> m_buffersToRelease;

        public ComputeBuffer Generate(ComputeBuffer pointsBuffer, int numPointsPerAxis, float chunkSize, Vector3 center,
            Vector3 offset, float voxelSize, Vector3 worldBounds)
        {
            m_buffersToRelease = new List<ComputeBuffer>();

            // 不同频率的波相位不同(随机产生offset来改变相位)
            var prng = new System.Random(seed);
            var offsets = new Vector3[octaves];
            float offsetRange = 1000;
            for (int i = 0; i < octaves; i++)
            { 
                offsets[i] = new Vector3((float) prng.NextDouble() * 2 - 1, (float) prng.NextDouble() * 2 - 1,(float) prng.NextDouble() * 2 - 1) * offsetRange;
               //offsets[i] = new Vector3(0.5f, 0.5f, 0.5f);
            }

            var offsetsBuffer = new ComputeBuffer(offsets.Length, sizeof(float) * 3);
            offsetsBuffer.SetData(offsets);
            m_buffersToRelease.Add(offsetsBuffer);

            //Noise FBM Settings
            NoiseDensityShader.SetVector("centre", center);
            NoiseDensityShader.SetInt("_Octaves", Mathf.Max (1, octaves));
            NoiseDensityShader.SetFloat("_Lacunarity", lacunarity);
            NoiseDensityShader.SetFloat("_Gain", gain);
            NoiseDensityShader.SetFloat("_Frequency", noiseFrequency);
            NoiseDensityShader.SetFloat("noiseWeight", noiseWeight);
            NoiseDensityShader.SetBuffer(0, "offsets", offsetsBuffer);
            NoiseDensityShader.SetFloat("isoLevelOffset", isoLevelOffset);
            //NoiseDensityShader.SetFloat("weightMultiplier", weightMultiplier);
            // NoiseDensityShader.SetFloat("hardFloor", hardFloorHeight);
            // NoiseDensityShader.SetFloat("hardFloorWeight", hardFloorWeight);
            //NoiseDensityShader.SetVector("params", shaderParams);

            //Noise Data
            NoiseDensityShader.SetBuffer(0, "points", pointsBuffer);
            NoiseDensityShader.SetInt("numPointsPerAxis", numPointsPerAxis);
            NoiseDensityShader.SetFloat("chunkSize", chunkSize);
            NoiseDensityShader.SetVector("center", center);
            NoiseDensityShader.SetVector("offset", offset);
            NoiseDensityShader.SetFloat("voxelSize", voxelSize);
            NoiseDensityShader.SetVector("worldSize", worldBounds);
            NoiseDensityShader.Dispatch(0, threadGroupSize, threadGroupSize, threadGroupSize);

            foreach (var c in m_buffersToRelease)
            {
                c.Release();
            }

            return pointsBuffer;
        }

        //如果直接调用MeshGenerator.Run则会在unity启动时报错SendMessage cannot be called during Awake, CheckConsistency, or OnValidate
        private void OnValidate()
        {
            GameObject.FindObjectOfType<MeshGenerator>().isSettingsUpdated=true;
        }
    }
}