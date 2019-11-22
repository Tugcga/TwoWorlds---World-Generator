using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WorldGenerator
{
    public class WG_Position : MonoBehaviour
    {
        public float visualRadius = 1.0f;
        public float visualHeight = 1.0f;
        public float visualEndRadius = 0.25f;
        public Color color = Color.yellow;

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            Handles.color = color;
            Vector3 center = transform.position;
            Handles.DrawWireDisc(center, Vector3.up, visualRadius);

            Handles.DrawLine(center, center + visualHeight * Vector3.up);
            Gizmos.color = color;
            Gizmos.DrawSphere(center + visualHeight * Vector3.up, visualEndRadius);
#endif
        }
    }
}
