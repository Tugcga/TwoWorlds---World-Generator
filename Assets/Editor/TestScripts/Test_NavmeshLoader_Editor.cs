using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WorldGenerator_Test
{
    [CustomEditor(typeof(Test_NavmeshLoader))]
    public class Test_NavmeshLoader_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            Test_NavmeshLoader nvmLoader = (Test_NavmeshLoader)target;

            DrawDefaultInspector();

            if (GUILayout.Button("Load data"))
            {
                nvmLoader.LoadNavmeshData();
            }
        }
    }


}
