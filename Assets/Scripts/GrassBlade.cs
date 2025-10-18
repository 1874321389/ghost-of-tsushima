using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tutorial404
{
    //在编辑器显示
    //[ExecuteInEditMode]
    public class GrassBlade : MonoBehaviour
    {
        public Material material;

        // Start is called before the first frame update
        void Start()
        {
            var clonedMesh = GrassMesh.CreateHighLODMesh();

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

            meshFilter.mesh = clonedMesh;
            meshRenderer.sharedMaterial = material;
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}
