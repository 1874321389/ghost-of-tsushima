using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace Tutorial402
{
    public class Voronoi : MonoBehaviour
    {
        [Header("Voronoi Settings")]
        [SerializeField] private Material voronoiMaterial;
        [SerializeField] private int textureWidth = 512;
        [SerializeField] private int textureHeight = 512;
        [Range(1, 60)] public int numClumps = 30;
        [Range(1, 20)] public int numClumpTypes = 4;

        private RenderTexture voronoiRenderTexture;

        void Start()
        {
            if (voronoiRenderTexture == null)
            {
                voronoiRenderTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                voronoiRenderTexture.enableRandomWrite = true;
                voronoiRenderTexture.Create();
            }
        }

        void Update()
        {
            if (voronoiMaterial == null) return;
            if (voronoiRenderTexture == null) return;

            voronoiMaterial.SetInt("_NumClumps", numClumps);
            voronoiMaterial.SetInt("_NumClumpTypes", numClumpTypes);

            Graphics.Blit(null, voronoiRenderTexture, voronoiMaterial, 0);
        }

        void OnGUI()
        {
            if (voronoiRenderTexture == null)
                return;

            GUI.DrawTexture(new Rect(20, 20, 512, 512), voronoiRenderTexture, ScaleMode.ScaleToFit);
        }

        void OnDestroy()
        {
            if (voronoiRenderTexture != null)
            {
                voronoiRenderTexture.Release();
                if (Application.isPlaying)
                    Destroy(voronoiRenderTexture);
                else
                    DestroyImmediate(voronoiRenderTexture);
            }
        }
    }
}