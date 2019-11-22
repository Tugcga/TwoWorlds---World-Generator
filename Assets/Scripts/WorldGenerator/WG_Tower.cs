using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WorldGenerator
{
    public class WG_Tower : MonoBehaviour
    {
        public float visualRadius = 1.0f;
        public float visualHeight = 1.0f;
        public float visualSize = 0.25f;
        public string towerName;
        public Color color = Color.red;

        public int towerType;

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            Handles.color = color;
            Vector3 center = transform.position;
            Handles.DrawWireDisc(center, Vector3.up, visualRadius);

            Handles.DrawLine(center, center + visualHeight * Vector3.up);
            Gizmos.color = color;
            Gizmos.DrawCube(center + visualHeight * Vector3.up, new Vector3(visualSize, visualSize * 2, visualSize));
#endif
        }
    }
}
