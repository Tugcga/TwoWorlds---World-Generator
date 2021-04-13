using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldGenerator
{
    public class GlobalLocationDataSO : ScriptableObject
    {
        public int minU;
        public int minV;
        public int maxU;
        public int maxV;

        public float size;

        public string navmeshName;
        public string navmeshLink;

        public List<Vector3> startPositions;
    }
}
