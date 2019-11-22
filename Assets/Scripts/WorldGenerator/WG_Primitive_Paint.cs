using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace WorldGenerator
{
    public class WG_Primitive_Paint : WG_Primitive_BaseNoise
    {
        public float height = 8f;
        public float shift = 0.0f;

        public WG_Painter paintComponent;

        public modeEnum mode;

        void OnDrawGizmos()
        {
            OnDrawGizmosBase(height, mode);
        }

        public override FloatBool GetHeight(Vector2 position)
        {
            FloatBool toReturn = new FloatBool { boolVal = false, floatVal = 0.0f };
            if (!isActive)
            {
                return toReturn;
            }
            else
            {
                Vector2 localPosition = new Vector2(transform.position.x, transform.position.z);
                if (mode == modeEnum.Area && Vector3.Distance(position, localPosition) > areaOuterRadius)
                {
                    return toReturn;
                }
                toReturn.boolVal = true;
                toReturn.floatVal = GetHeightInner(position);
                return toReturn;
            }
        }

        public override float GetHeightInner(Vector2 position)
        {
            float value = 0.0f;
            if (paintComponent != null)
            {
                value = paintComponent.GetHeight(position);
            }
            
            Vector2 localPosition = new Vector2(transform.position.x, transform.position.z);
            float toCenter = Vector3.Distance(position, localPosition);

            float coefficient = 1f;
            if (mode == modeEnum.Area)
            {
                if (toCenter > areaInnerRadius)
                {
                    coefficient = GetAreaProfileValue(0, 1, 1 - (toCenter - areaInnerRadius) / (areaOuterRadius - areaInnerRadius));
                }
            }
            return value * coefficient * height;
        }
        
    }
}
