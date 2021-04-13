using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldGenerator
{
    [System.Serializable]
    public class ItemSO
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public string meshName;
        public string meshLink;

        public string materialName;
        public string materialLink;
    }

    public class LocationDataSO : ScriptableObject
    {
        public List<ItemSO> items;

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public string minimapName;
        public string minimapLink;
    }
}
