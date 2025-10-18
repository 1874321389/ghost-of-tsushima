using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tutorial303
{
    public class Grass : MonoBehaviour
    {
        [SerializeField]
        private ComputeShader computeShader;
        [SerializeField]
        private Material material;
        public Camera cam;
        public float grassSpacing = 0.1f;
        public int resolution = 100;
        [SerializeField, Range(0, 2)]
        public float jitterStrength;

        private static readonly int
            grassBladesBufferID = Shader.PropertyToID("_GrassBlades"),
            resolutionID = Shader.PropertyToID("_Resolution"),
            grassSpacingID = Shader.PropertyToID("_GrassSpacing"),
            jitterStrengthID = Shader.PropertyToID("_JitterStrength");

        private ComputeBuffer grassBladesBuffer;
        private ComputeBuffer meshTrianglesBuffer;
        private ComputeBuffer meshPositionsBuffer;
        private ComputeBuffer meshColorsBuffer;
        private ComputeBuffer meshUVsBuffer;
        private ComputeBuffer argsBuffer;
        private const int ARGS_STRIDE = sizeof(int) * 4;
        private Mesh clonedMesh;
        private Bounds bounds;

        void Awake()
        {
            Initialize();
            bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
        }

        void Start()
        {
        }

        void Update()
        {
            UpdateGpuParameters();
        }

        void OnDestroy()
        {
            DisposeBuffers();
        }

        private void Initialize()
        {
            InitializeComputeBuffers();
            SetupMeshBuffers();
        }

        private void InitializeComputeBuffers()
        {
            grassBladesBuffer = new ComputeBuffer(resolution * resolution, sizeof(float) * 3, ComputeBufferType.Append);
            grassBladesBuffer.SetCounterValue(0);
            argsBuffer = new ComputeBuffer(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);
        }

        private void SetupMeshBuffers()
        {
            clonedMesh = GrassMesh.CreateHighLODMesh();
            clonedMesh.name = "Grass Instance Mesh";

            CreateComputeBuffersForMesh();
            argsBuffer.SetData(new int[] { meshTrianglesBuffer.count, 0, 0, 0 });
        }

        private ComputeBuffer CreateBuffer<T>(T[] data, int stride) where T : struct
        {
            ComputeBuffer buffer = new ComputeBuffer(data.Length, stride);
            buffer.SetData(data);
            return buffer;
        }

        private void CreateComputeBuffersForMesh()
        {
            int[] triangles = clonedMesh.triangles;
            Vector3[] positions = clonedMesh.vertices;
            Color[] colors = clonedMesh.colors;
            Vector2[] uvs = clonedMesh.uv;

            meshTrianglesBuffer = CreateBuffer<int>(triangles, sizeof(int));
            meshPositionsBuffer = CreateBuffer<Vector3>(positions, sizeof(float) * 3);
            meshColorsBuffer = CreateBuffer<Color>(colors, sizeof(float) * 4);
            meshUVsBuffer = CreateBuffer<Vector2>(uvs, sizeof(float) * 2);

            material.SetBuffer("Triangles", meshTrianglesBuffer);
            material.SetBuffer("Positions", meshPositionsBuffer);
            material.SetBuffer("Colors", meshColorsBuffer);
            material.SetBuffer("UVs", meshUVsBuffer);
            material.SetBuffer(grassBladesBufferID, grassBladesBuffer);
        }

        private void UpdateGpuParameters()
        {
            grassBladesBuffer.SetCounterValue(0);

            SetupComputeShader();

            int threadGroupsX = Mathf.CeilToInt(resolution / 8f);
            int threadGroupsZ = Mathf.CeilToInt(resolution / 8f);
            computeShader.Dispatch(0, threadGroupsX, threadGroupsZ, 1);

            RenderGrass();
        }

        private void SetupComputeShader()
        {
            computeShader.SetInt(resolutionID, resolution);
            computeShader.SetBuffer(0, grassBladesBufferID, grassBladesBuffer);
            computeShader.SetFloat(grassSpacingID, grassSpacing);
            computeShader.SetFloat(jitterStrengthID, jitterStrength);
        }

        private void RenderGrass()
        {
            ComputeBuffer.CopyCount(grassBladesBuffer, argsBuffer, sizeof(int));

            Graphics.DrawProceduralIndirect(material, bounds, MeshTopology.Triangles, argsBuffer,
                0, cam, null, UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
        }

        private void DisposeBuffers()
        {
            DisposeBuffer(grassBladesBuffer);
            DisposeBuffer(meshTrianglesBuffer);
            DisposeBuffer(meshPositionsBuffer);
            DisposeBuffer(meshColorsBuffer);
            DisposeBuffer(meshUVsBuffer);
            DisposeBuffer(argsBuffer);
        }

        private void DisposeBuffer(ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Dispose();
                buffer = null;
            }
        }
    }
}