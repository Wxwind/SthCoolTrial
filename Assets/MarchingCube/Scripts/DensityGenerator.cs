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
        public int numOctaves = 4;
        public float lacunarity = 2;
        public float persistence = .5f;
        public float noiseScale = 1; //frequency的缩放系数
        public float noiseWeight = 1;
        public bool closeEdges;
        public float floorOffset = 1;
        public float weightMultiplier = 1;

        public float hardFloorHeight;
        public float hardFloorWeight;

        public Vector4 shaderParams;

        [ShowInInspector] const int threadGroupSize = 8;

        public ComputeShader NoiseDensityShader;
        private List<ComputeBuffer> m_buffersToRelease;

        public ComputeBuffer Generate(ComputeBuffer pointsBuffer, int numPointsPerAxis, float chunkSize, Vector3 center,
            Vector3 offset, float voxelSize, Vector3 worldBounds)
        {
            m_buffersToRelease = new List<ComputeBuffer>();

            // Noise parameters
            var prng = new System.Random(seed);
            var offsets = new Vector3[numOctaves];
            float offsetRange = 1000;
            for (int i = 0; i < numOctaves; i++)
            { 
                offsets[i] = new Vector3((float) prng.NextDouble() * 2 - 1, (float) prng.NextDouble() * 2 - 1,(float) prng.NextDouble() * 2 - 1) * offsetRange;
               //offsets[i] = new Vector3(0.5f, 0.5f, 0.5f);
            }

            var offsetsBuffer = new ComputeBuffer(offsets.Length, sizeof(float) * 3);
            offsetsBuffer.SetData(offsets);
            m_buffersToRelease.Add(offsetsBuffer);

            //Noise FBM Settings
            NoiseDensityShader.SetVector("centre", center);
            NoiseDensityShader.SetInt("octaves", Mathf.Max (1, numOctaves));
            NoiseDensityShader.SetFloat("lacunarity", lacunarity);
            NoiseDensityShader.SetFloat("persistence", persistence);
            NoiseDensityShader.SetFloat("noiseScale", noiseScale);
            NoiseDensityShader.SetFloat("noiseWeight", noiseWeight);
            NoiseDensityShader.SetBuffer(0, "offsets", offsetsBuffer);
            NoiseDensityShader.SetFloat("floorOffset", floorOffset);
            NoiseDensityShader.SetFloat("weightMultiplier", weightMultiplier);
            NoiseDensityShader.SetFloat("hardFloor", hardFloorHeight);
            NoiseDensityShader.SetFloat("hardFloorWeight", hardFloorWeight);
            NoiseDensityShader.SetVector("params", shaderParams);

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

        private void OnValidate()
        {
            GameObject.FindObjectOfType<MeshGenerator>().Run();
        }
    }
}