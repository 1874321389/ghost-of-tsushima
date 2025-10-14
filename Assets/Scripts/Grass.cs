using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tutorial301
{
    public class Grass : MonoBehaviour
    {
        [SerializeField]
        private ComputeShader computeShader;

        public float grassSpacing = 0.1f;
        public int resolution = 100;

        private static readonly int
            grassBladesBufferID = Shader.PropertyToID("_GrassBlades"),
            resolutionID = Shader.PropertyToID("_Resolution"),
            grassSpacingID = Shader.PropertyToID("_GrassSpacing");

        private ComputeBuffer grassBladesBuffer;

        void Awake()
        {
            Initialize();
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
        }

        private void InitializeComputeBuffers()
        {
            grassBladesBuffer = new ComputeBuffer(resolution * resolution, sizeof(float) * 3, ComputeBufferType.Append);
            grassBladesBuffer.SetCounterValue(0);
        }

        private void UpdateGpuParameters()
        {
            grassBladesBuffer.SetCounterValue(0);

            SetupComputeShader();

            int threadGroupsX = Mathf.CeilToInt(resolution / 8f);
            int threadGroupsZ = Mathf.CeilToInt(resolution / 8f);
            computeShader.Dispatch(0, threadGroupsX, threadGroupsZ, 1);
        }

        private void SetupComputeShader()
        {
            computeShader.SetInt(resolutionID, resolution);
            computeShader.SetBuffer(0, grassBladesBufferID, grassBladesBuffer);
            computeShader.SetFloat(grassSpacingID, grassSpacing);
        }

        private void DisposeBuffers()
        {
            DisposeBuffer(grassBladesBuffer);
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