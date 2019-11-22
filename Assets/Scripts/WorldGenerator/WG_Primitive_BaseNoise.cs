using UnityEngine;
using UnityEditor;

namespace WorldGenerator
{
    public class WG_Primitive_BaseNoise : WG_Primitive
    {
        public float areaOuterRadius;
        public float areaInnerRadius;

        [Range(0, 128)]
        public float iconSize = 10f;
        [Range(2, 128)]
        public int iconNoiseSteps = 16;

        public void OnDrawGizmosBase(float height, modeEnum mode)
        {
#if UNITY_EDITOR
            if (mode == modeEnum.Area)
            {
                if (isActive)
                {
                    if (Selection.activeGameObject == gameObject)
                    {
                        Handles.color = Color.green;
                    }
                    else
                    {
                        Handles.color = Color.yellow;
                    }
                }
                else
                {
                    Handles.color = Color.gray;
                }
                if (areaInnerRadius < 0)
                {
                    areaInnerRadius = 0;
                }
                if (areaOuterRadius < 0)
                {
                    areaOuterRadius = 0;
                }
                if (areaOuterRadius < areaInnerRadius)
                {
                    areaInnerRadius = areaOuterRadius;
                }

                Vector3 center = transform.position;
                Handles.DrawWireDisc(center, Vector3.up, areaOuterRadius);
                Vector3[] innerPoints = new Vector3[iconNoiseSteps + 1];
                Vector2 local = new Vector2(transform.position.x, transform.position.z);
                float angleStep = 2 * Mathf.PI / iconNoiseSteps;
                for (int i = 0; i < iconNoiseSteps; i++)
                {
                    Vector2 point = new Vector2(areaInnerRadius * Mathf.Cos(i * angleStep), areaInnerRadius * Mathf.Sin(i * angleStep)) + local;
                    innerPoints[i] = new Vector3(point.x, GetHeightInner(point), point.y);
                }
                innerPoints[iconNoiseSteps] = innerPoints[0];
                Handles.DrawPolyLine(innerPoints);
                for (int i = 0; i < iconNoiseSteps; i++)
                {
                    Handles.DrawLine(new Vector3(areaOuterRadius * Mathf.Cos(i * angleStep), 0f, areaOuterRadius * Mathf.Sin(i * angleStep)) + center, innerPoints[i]);
                }
            }
            else if (mode == modeEnum.Infinite)
            {
                if (isActive)
                {
                    if (Selection.activeGameObject == gameObject)
                    {
                        Handles.color = Color.green;
                    }
                    else
                    {
                        Handles.color = Color.yellow;
                    }
                }
                else
                {
                    Handles.color = Color.gray;
                }

                Vector3 localPosition = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3[] basePoints = new Vector3[5];
                basePoints[0] = new Vector3(-1 * iconSize / 2, 0, -1 * iconSize / 2) + localPosition;
                basePoints[1] = new Vector3(-1 * iconSize / 2, 0, iconSize / 2) + localPosition;
                basePoints[2] = new Vector3(iconSize / 2, 0, iconSize / 2) + localPosition;
                basePoints[3] = new Vector3(iconSize / 2, 0, -1 * iconSize / 2) + localPosition;
                basePoints[4] = new Vector3(-1 * iconSize / 2, 0, -1 * iconSize / 2) + localPosition;
                Handles.DrawPolyLine(basePoints);

                Vector2 point0 = new Vector2(transform.position.x - iconSize / 2, transform.position.z - iconSize / 2);
                Vector2 local = new Vector2(transform.position.x, transform.position.z);
                Vector2 point0Local = new Vector2(-iconSize / 2, -iconSize / 2) + local;
                Vector2 point1 = new Vector2(transform.position.x - iconSize / 2, transform.position.z + iconSize / 2);
                Vector2 point1Local = new Vector2(-iconSize / 2, iconSize / 2) + local;
                Vector2 point2Local = new Vector2(iconSize / 2, iconSize / 2) + local;
                Vector2 point3 = new Vector2(transform.position.x + iconSize / 2, transform.position.z - iconSize / 2);
                Vector2 point3Local = new Vector2(iconSize / 2, -iconSize / 2) + local;
                //draw lines along z direction
                for (int i = 0; i < iconNoiseSteps; i++)
                {
                    float t = i / (float)(iconNoiseSteps - 1);
                    Vector3[] line = new Vector3[iconNoiseSteps];
                    for (int j = 0; j < iconNoiseSteps; j++)
                    {
                        float u = j / (float)(iconNoiseSteps - 1);
                        line[j] = new Vector3(point0.x * (1 - t) + point3.x * t, GetHeightInner((point0Local * (1 - t) + point3Local * t) * (1 - u) + (point1Local * (1 - t) + point2Local * t) * u), point0.y * (1 - u) + point1.y * u);
                    }
                    Handles.DrawPolyLine(line);
                }
                //also draw lines in x direction
                for (int i = 0; i < iconNoiseSteps; i++)
                {
                    float t = i / (float)(iconNoiseSteps - 1);
                    Vector3[] line = new Vector3[iconNoiseSteps];
                    for (int j = 0; j < iconNoiseSteps; j++)
                    {
                        float u = j / (float)(iconNoiseSteps - 1);
                        line[j] = new Vector3(point0.x * (1 - u) + point3.x * u, GetHeightInner((point0Local * (1 - t) + point1Local * t) * (1 - u) + (point3Local * (1 - t) + point2Local * t) * u), point0.y * (1 - t) + point1.y * t);
                    }
                    Handles.DrawPolyLine(line);
                }
                Handles.DrawLine(basePoints[0], new Vector3(basePoints[0].x, GetHeightInner(point0Local), basePoints[0].z));
                Handles.DrawLine(basePoints[1], new Vector3(basePoints[1].x, GetHeightInner(point1Local), basePoints[1].z));
                Handles.DrawLine(basePoints[2], new Vector3(basePoints[2].x, GetHeightInner(point2Local), basePoints[2].z));
                Handles.DrawLine(basePoints[3], new Vector3(basePoints[3].x, GetHeightInner(point3Local), basePoints[3].z));
            }
#endif
            Gizmos.DrawIcon(transform.position, "gizmos_mountains_01.png", false);
        }

        public virtual float GetHeightInner(Vector2 position)
        {
            return 0f;
        }
    }

}
