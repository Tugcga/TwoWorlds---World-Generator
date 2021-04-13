using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WorldGenerator
{
    [CustomEditor(typeof(WG_LocationsExporter))]
    public class WG_LocationsExporter_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            WG_LocationsExporter wgExporter = (WG_LocationsExporter) target;

            DrawDefaultInspector();

            if (GUILayout.Button("Clear asset folders"))
            {
                wgExporter.ClearAssetFolders();
            }

            if (GUILayout.Button("3. Prepare Scene"))
            {
                wgExporter.PrepareScene();
            }

            if (GUILayout.Button("3.5. Prepare Navmesh only"))
            {
                wgExporter.PrepareNavmesh();
            }

            if (GUILayout.Button("4. Export Scene"))
            {
                wgExporter.ExportScene();
            }
        }
    }
}
