using System;
using UnityEngine;

namespace WorldGenerator
{
    [ExecuteInEditMode]
    public class WG_Primitive : MonoBehaviour
    {
        public bool isActive = true;
        public string stringId = "";
        public AnimationCurve noiseProfileCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve areaProfileCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public additiveEnum additive;
        public float importantLevel;

        private void Start()
        {
            if (stringId == "")
            {
                GenerateID();
            }
            else
            {
                WG_Primitive[] existObjects = FindObjectsOfType<WG_Primitive>();
                //Find all object with the same id
                int objCount = 0;
                for (int i = 0; i < existObjects.Length; i++)
                {
                    if (existObjects[i].stringId == stringId)
                    {
                        objCount++;
                    }
                }
                if (objCount > 1)
                {
                    GenerateID();
                }
            }
        }

        private void GenerateID()
        {
            Guid guid = Guid.NewGuid();
            stringId = guid.ToString();
        }

        public float GetNoiseProfileValue(float minValue, float maxValue, float currentValue)
        {
            if (currentValue < minValue)
            {
                currentValue = minValue;
            }
            if (currentValue > maxValue)
            {
                currentValue = maxValue;
            }
            return noiseProfileCurve.Evaluate((currentValue - minValue) / Mathf.Max(maxValue - minValue, 0.01f));
        }

        public float GetAreaProfileValue(float minValue, float maxValue, float currentValue)
        {
            return areaProfileCurve.Evaluate((currentValue - minValue) / Mathf.Max(maxValue - minValue, 0.01f));
        }

        public virtual FloatBool GetHeight(Vector2 position)
        {
            return new FloatBool { boolVal = false, floatVal = 0.0f };
        }
    }

}
