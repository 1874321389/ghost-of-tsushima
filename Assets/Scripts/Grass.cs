using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tutorial404
{
    public class Grass : MonoBehaviour
    {
        [Header("Core Settings")]
        [SerializeField] 
        private ComputeShader computeShader;
        [SerializeField] 
        private Material material;
        public Camera cam;

        public float grassSpacing = 0.1f;
        public int resolution = 100;

        [SerializeField, Range(0, 2)] 
        public float jitterStrength;
        public Terrain terrain;

        [Header("Culling")]
        public float distanceCullStartDistance;
        public float distanceCullEndDistance;
        [Range(0, 1)] 
        public float distanceCullMinimumGrassAmount;
        public float frustumCullNearOffset;
        public float frustumCullEdgeOffset;

        [Header("Clumping")]
        public int clumpTextureWidth;
        public int clumpTextureHeight;
        public Material clumpingVoronoiMaterial;
        public float clumpScale;
        public List<ClumpParameters> clumpParameters;

        private static readonly int
            grassBladesBufferID = Shader.PropertyToID("_GrassBlades"),
            resolutionID = Shader.PropertyToID("_Resolution"),
            grassSpacingID = Shader.PropertyToID("_GrassSpacing"),
            jitterStrengthID = Shader.PropertyToID("_JitterStrength"),
            terrainPositionID = Shader.PropertyToID("_TerrainPosition"),
            heightMapID = Shader.PropertyToID("_HeightMap"),
            detailMapID = Shader.PropertyToID("_DetailMap"),
            heightMapScaleID = Shader.PropertyToID("_HeightMapScale"),
            heightMapMultiplierID = Shader.PropertyToID("_HeightMapMultiplier"),
            distanceCullStartDistID = Shader.PropertyToID("_DistanceCullStartDist"),
            distanceCullEndDistID = Shader.PropertyToID("_DistanceCullEndDist"),
            distanceCullMinimumGrassAmountID = Shader.PropertyToID("_DistanceCullMinimumGrassAmount"),
            worldSpaceCameraPositionID = Shader.PropertyToID("_WSpaceCameraPos"),
            vpMatrixID = Shader.PropertyToID("_VP_MATRIX"),
            frustumCullNearOffsetID = Shader.PropertyToID("_FrustumCullNearOffset"),
            frustumCullEdgeOffsetID = Shader.PropertyToID("_FrustumCullEdgeOffset"),
            clumpParametersID = Shader.PropertyToID("_ClumpParameters"),
            numClumpParametersID = Shader.PropertyToID("_NumClumpParameters"),
            clumpTexID = Shader.PropertyToID("_ClumpTex"),
            clumpScaleID = Shader.PropertyToID("_ClumpScale");

        private ComputeBuffer grassBladesBuffer; //生成的草实例位置
        private ComputeBuffer meshTrianglesBuffer; //Grass 网格的三角形索引
        private ComputeBuffer meshColorsBuffer; //顶点颜色
        private ComputeBuffer meshUVsBuffer;
        private ComputeBuffer argsBuffer; //用于 DrawProceduralIndirect() 的核心参数缓存
        private ComputeBuffer clumpParametersBuffer;
        private const int ARGS_STRIDE = sizeof(int) * 5;
        private Mesh clonedMesh;
        private Bounds bounds;
        private ClumpParameters[] clumpParametersArray;
        private Texture2D clumpTexture;

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

        void LateUpdate()
        {
            RenderGrass();
        }

        void OnDestroy()
        {
            DisposeBuffers();
            DestroyClumpTexture();
        }

        private void Initialize()
        {
            InitializeComputeBuffers();
            SetupMeshBuffers();
            CreateClumpTexture();
        }

        private void InitializeComputeBuffers()
        {
            grassBladesBuffer = new ComputeBuffer(resolution * resolution, sizeof(float) * 14, ComputeBufferType.Append);
            grassBladesBuffer.SetCounterValue(0);
            
            argsBuffer = new ComputeBuffer(1, ARGS_STRIDE, ComputeBufferType.IndirectArguments);

            clumpParametersBuffer = new ComputeBuffer(clumpParameters.Count, sizeof(float) * 10);
            UpdateClumpParametersBuffer();
        }

        private void SetupMeshBuffers()
        {
            clonedMesh = GrassMesh.CreateHighLODMesh();
            clonedMesh.name = "Grass Instance Mesh";

            CreateComputeBuffersForMesh();
            argsBuffer.SetData(new int[] { meshTrianglesBuffer.count, 0, 0, 0, 0 });
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
            Color[] colors = clonedMesh.colors;
            Vector2[] uvs = clonedMesh.uv;

            meshTrianglesBuffer = CreateBuffer<int>(triangles, sizeof(int));
            meshColorsBuffer = CreateBuffer<Color>(colors, sizeof(float) * 4);
            meshUVsBuffer = CreateBuffer<Vector2>(uvs, sizeof(float) * 2);

            material.SetBuffer("Triangles", meshTrianglesBuffer);
            material.SetBuffer("Colors", meshColorsBuffer);
            material.SetBuffer("UVs", meshUVsBuffer);
            material.SetBuffer(grassBladesBufferID, grassBladesBuffer);
        }
        private void CreateClumpTexture()
        {
            clumpingVoronoiMaterial.SetFloat("_NumClumpTypes", clumpParameters.Count);
            RenderTexture clumpVoronoiRenderTexture = RenderTexture.GetTemporary(
                clumpTextureWidth, clumpTextureHeight, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            Graphics.Blit(null, clumpVoronoiRenderTexture, clumpingVoronoiMaterial, 0);

            RenderTexture.active = clumpVoronoiRenderTexture;
            clumpTexture = new Texture2D(clumpTextureWidth, clumpTextureHeight, TextureFormat.RGBAHalf, false, true);
            clumpTexture.filterMode = FilterMode.Point;
            clumpTexture.ReadPixels(new Rect(0, 0, clumpTextureWidth, clumpTextureHeight), 0, 0, true);
            clumpTexture.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(clumpVoronoiRenderTexture);
        }

        private void UpdateGpuParameters()
        {
            grassBladesBuffer.SetCounterValue(0); //重置 AppendBuffer 计数器

            SetupComputeShader(); //向cs传递参数

            int threadGroupsX = Mathf.CeilToInt(resolution / 8f);
            int threadGroupsZ = Mathf.CeilToInt(resolution / 8f);
            computeShader.Dispatch(0, threadGroupsX, threadGroupsZ, 1); //运行第 0 个 kernel

            RenderGrass();
        }
        private void UpdateClumpParametersBuffer()
        {
            if (clumpParameters.Count > 0)
            {
                if (clumpParametersArray == null || clumpParametersArray.Length != clumpParameters.Count)
                {
                    clumpParametersArray = new ClumpParameters[clumpParameters.Count];
                }
                clumpParameters.CopyTo(clumpParametersArray);
                clumpParametersBuffer.SetData(clumpParametersArray);
            }
        }

        private void SetupComputeShader()
        {
            computeShader.SetInt(resolutionID, resolution);
            computeShader.SetBuffer(0, grassBladesBufferID, grassBladesBuffer);
            computeShader.SetFloat(grassSpacingID, grassSpacing);
            computeShader.SetFloat(jitterStrengthID, jitterStrength);

            if (terrain != null)
            {
                computeShader.SetVector(terrainPositionID, terrain.transform.position);
                computeShader.SetTexture(0, heightMapID, terrain.terrainData.heightmapTexture);
                if (terrain.terrainData.alphamapTextures.Length > 0)
                {
                    computeShader.SetTexture(0, detailMapID, terrain.terrainData.alphamapTextures[0]);
                }

                computeShader.SetFloat(heightMapScaleID, terrain.terrainData.size.x);
                computeShader.SetFloat(heightMapMultiplierID, terrain.terrainData.size.y);
            }

            computeShader.SetFloat(distanceCullStartDistID, distanceCullStartDistance);
            computeShader.SetFloat(distanceCullEndDistID, distanceCullEndDistance);
            computeShader.SetFloat(distanceCullMinimumGrassAmountID, distanceCullMinimumGrassAmount);
            computeShader.SetFloat(frustumCullNearOffsetID, frustumCullNearOffset);
            computeShader.SetFloat(frustumCullEdgeOffsetID, frustumCullEdgeOffset);

            computeShader.SetVector(worldSpaceCameraPositionID, cam.transform.position);

            Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
            Matrix4x4 viewProjectionMatrix = projectionMatrix * cam.worldToCameraMatrix;
            computeShader.SetMatrix(vpMatrixID, viewProjectionMatrix);

            UpdateClumpParametersBuffer();
            computeShader.SetBuffer(0, clumpParametersID, clumpParametersBuffer);
            computeShader.SetFloat(numClumpParametersID, clumpParameters.Count);
            computeShader.SetTexture(0, clumpTexID, clumpTexture);
            computeShader.SetFloat(clumpScaleID, clumpScale);
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
            DisposeBuffer(meshColorsBuffer);
            DisposeBuffer(meshUVsBuffer);
            DisposeBuffer(argsBuffer);
            DisposeBuffer(clumpParametersBuffer);
        }

        private void DisposeBuffer(ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Dispose();
                buffer = null;
            }
        }

        private void DestroyClumpTexture()
        {
            if (clumpTexture != null)
            {
                Destroy(clumpTexture);
                clumpTexture = null;
            }
        }
    }
}