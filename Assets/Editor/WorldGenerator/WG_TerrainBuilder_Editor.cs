using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WorldGenerator
{
    [CustomEditor(typeof(WG_TerrainBuilder))]
    public class WG_TerrainBuilder_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            WG_TerrainBuilder wgBuilder = (WG_TerrainBuilder)target;

            DrawDefaultInspector();
            
            if (GUILayout.Button("Update Map"))
            {
                wgBuilder.UpdateMap();
            }

            if (GUILayout.Button("Build navmesh only"))
            {
                wgBuilder.BuildNavmeshOnly();
            }

            if (GUILayout.Button("1. Build Scene"))
            {
                wgBuilder.BuildMesh();
            }
        }
    }
}
