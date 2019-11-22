using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldGenerator
{
    [System.Serializable]
    public class WG_Painter : MonoBehaviour
    {
        public List<Disc> points = new List<Disc>();
        private int currentLevel = 0;
        public WG_TerrainBuilder wgBuilder;
        public float brushRadius = 7.0f;
        public float strokeStep = 5.0f;
        public Color brushColor = new Color(1.0f, 0.65f, 0.0f, 0.25f);
        public float pointsAlpha = 0.05f;
        public float pointsSize = 0.1f;
        public float brushScale = 1.0f;
        public bool drawPoints = false;

        public void Clear()
        {
            points.Clear();
            wgBuilder.UpdateMap();
        }

        public int GetPoinsCount()
        {
            return points.Count;
        }

        public void RemovePoints(Vector2 center, float radius)
        {
            for (int i = points.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(points[i].center, center) < radius)
                {
                    points.RemoveAt(i);
                }
            }
        }

        public void AddPoint(Vector2 center, float radius, bool isNegative)
        {
            points.Add(new Disc() {center = center, radius = radius, isNegative = isNegative, level = currentLevel});
            currentLevel++;
        }

        public float GetHeight(Vector2 point)
        {
            int maxIndex = -1;
            bool isHeight = false;
            for (int i = 0; i < points.Count; i++)
            {
                Disc d = points[i];
                if (WG_Helper.IsInside(point, d))
                {
                    if (d.level > maxIndex)
                    {
                        maxIndex = d.level;
                        isHeight = !d.isNegative;
                    }
                }
            }

            return isHeight ? 1.0f : 0.0f;
        }

    }
}

