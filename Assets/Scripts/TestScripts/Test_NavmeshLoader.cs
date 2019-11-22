using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace WorldGenerator_Test
{
    public class Test_NavmeshLoader : MonoBehaviour
    {
        public NavMeshData navmeshData;

        void Start()
        {

        }

        void Update()
        {

        }

        public void LoadNavmeshData()
        {
            NavMesh.AddNavMeshData(navmeshData);
        }
    }
}
