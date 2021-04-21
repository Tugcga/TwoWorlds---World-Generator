using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WorldGenerator_Test
{
    [CustomEditor(typeof(Test_ConvertLightmap))]
    public class Test_ConvertLightmap_editor : Editor
    {
        public override void OnInspectorGUI()
        {
            Test_ConvertLightmap converter = (Test_ConvertLightmap)target;

            DrawDefaultInspector();

            if (GUILayout.Button("Convert"))
            {
                converter.Convert();
            }
        }
    }

}