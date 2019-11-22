using UnityEngine;

namespace WorldGenerator
{
    public class WG_Primitive_PerlinNoise : WG_Primitive_BaseNoise
    {
        public float scaleMultiplier = 0.05f;
        [Range(0, 2)]
        public float scaleRange = 1f;
        public Vector2 center = new Vector2(20, 20);
        [Range(1, 8)]
        public int octaves = 1;
        public float octaveScaleReduceFactor = 2f;
        public float octaveHeightReduceFactor = 3f;
        public float height = 8f;
        public float shift = 0.0f;

        public modeEnum mode;

        public noiseTypeEnum noiseType;

        void OnDrawGizmos()
        {
            OnDrawGizmosBase(height, mode);
        }

        public override FloatBool GetHeight(Vector2 position)
        {
            FloatBool toReturn = new FloatBool {boolVal = false, floatVal = 0.0f};
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

        Vector2 ApplyRotation(Vector2 vector, float angle)
        {
            return new Vector2(vector.x * Mathf.Cos(angle) - vector.y * Mathf.Sin(angle), vector.x * Mathf.Sin(angle) + vector.y * Mathf.Cos(angle));
        }

        public override float GetHeightInner(Vector2 position)
        {
            Vector2 localPosition = new Vector2(transform.position.x, transform.position.z);
            float angle = transform.eulerAngles.y * Mathf.PI / 180.0f;

            float toCenter = Vector3.Distance(position, localPosition);
            if (mode == modeEnum.Area && Vector3.Distance(position, localPosition) > areaOuterRadius)
            {
                return 0f;
            }

            position = ApplyRotation(position, angle);
            localPosition = ApplyRotation(localPosition, angle);

            Vector2 point = position + center - localPosition;
            //point = new Vector2(point.x * Mathf.Cos(angle) - point.y * Mathf.Sin(angle), point.x * Mathf.Sin(angle) + point.y * Mathf.Cos(angle));
            float scale = scaleMultiplier * scaleRange;
            float noiseValue = 0;
            float curentScale = scale;
            float curentHeight = height;
            for (int oIndex = 0; oIndex < octaves; oIndex++)
            {
                noiseValue += GetNoiseProfileValue(0, 1, Mathf.PerlinNoise(point.x * curentScale, point.y * curentScale)) * curentHeight;
                curentScale = curentScale * octaveScaleReduceFactor;
                curentHeight = curentHeight / octaveHeightReduceFactor;
            }
            noiseValue = noiseValue + shift;
            if (noiseValue < 0.0f)
            {
                noiseValue = 0.0f;
            }

            float coefficient = 1f;
            if (mode == modeEnum.Area)
            {
                if (toCenter > areaInnerRadius)
                {
                    coefficient = GetAreaProfileValue(0, 1, 1 - (toCenter - areaInnerRadius) / (areaOuterRadius - areaInnerRadius));
                }
            }
            return noiseValue * coefficient;
        }
    }
}
