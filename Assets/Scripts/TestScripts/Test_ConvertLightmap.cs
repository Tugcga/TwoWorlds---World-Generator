using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.IO;
using UnityEditor;

namespace WorldGenerator_Test
{
    public class Test_ConvertLightmap : MonoBehaviour
    {
        public Texture2D lightmapTexture;

        public string outputPath;

        [Range(0, 10)]
        public float decodeInstructionsX;
        [Range(0, 10)]
        public float decodeInstructionsY;

        public bool saveExr;

        float GammaToLinearSpaceExact(float value)
        {
            if (value <= 0.04045F)
                return value / 12.92F;
            else if (value < 1.0F)
                return Mathf.Pow((value + 0.055F) / 1.055F, 2.4F);
            else
                return Mathf.Pow(value, 2.2F);
        }

        Color GammaToLinearSpaceExact(Color value)
        {
            return new Color(GammaToLinearSpaceExact(value.r), GammaToLinearSpaceExact(value.g), GammaToLinearSpaceExact(value.b), value.a);
        }

        Color sqrt(Color color)
        {
            return new Color(Mathf.Sqrt(color.r), Mathf.Sqrt(color.g), Mathf.Sqrt(color.b), color.a);
        }

        Color GetMaxDifference(Color[] colorsA, Color[] colorsB)
        {
            Color toReturn = new Color(0f, 0f, 0f, 0f);
            for(int i = 0; i < colorsA.Length; i++)
            {
                float ra = colorsA[i].r;
                float ga = colorsA[i].g;
                float ba = colorsA[i].b;
                float aa = colorsA[i].a;

                float rb = colorsB[i].r;
                float gb = colorsB[i].g;
                float bb = colorsB[i].b;
                float ab = colorsB[i].a;

                float dr = Mathf.Abs(ra - rb);
                float dg = Mathf.Abs(ga - gb);
                float db = Mathf.Abs(ba - bb);
                float da = Mathf.Abs(aa - ab);

                if(toReturn.r < dr){toReturn.r = dr;}
                if (toReturn.g < dg) { toReturn.g = dg; }
                if (toReturn.b < db) { toReturn.b = db; }
                if (toReturn.a < da) { toReturn.a = da; }
            }

            return toReturn;
        }

        public void Convert()
        {
#if UNITY_EDITOR
            int width = lightmapTexture.width;
            int height = lightmapTexture.height;
            Texture2D newTexture = new Texture2D(width, height, DefaultFormat.HDR, TextureCreationFlags.None);

            Color[] lmColorsRaw = lightmapTexture.GetPixels(0, 0, width, height);
            Color[] colors = new Color[width * height];
            for(int i = 0; i < lmColorsRaw.Length; i++)
            {
                //colors[i] = lmColorsRaw[i].linear;
                //colors[i] *= 0.45f;
                //colors[i].r = GammaToLinearSpaceExact(lmColorsRaw[i].r);
                //colors[i].g = GammaToLinearSpaceExact(lmColorsRaw[i].g);
                //colors[i].b = GammaToLinearSpaceExact(lmColorsRaw[i].b);

                //colors[i] = (2.0f * lmColorsRaw[i].a) * sqrt(lmColorsRaw[i]);
                //colors[i] = (decodeInstructionsX * Mathf.Pow(lmColorsRaw[i].a, decodeInstructionsY)) * lmColorsRaw[i];
                //colors[i] = GammaToLinearSpaceExact(lmColorsRaw[i]);
                colors[i] = lmColorsRaw[i].linear;
            }

            Debug.Log("Difference: " + GetMaxDifference(lmColorsRaw, colors).ToString());

            //may be we need some color conversation

            newTexture.SetPixels(0, 0, width, height, colors);
            newTexture.Apply();

            string filePath = Application.dataPath + "/" + outputPath;

            if(saveExr)
            {
                byte[] _bytes = ImageConversion.EncodeToEXR(newTexture, Texture2D.EXRFlags.CompressZIP);
                File.WriteAllBytes(filePath, _bytes);
            }
            else
            {
                byte[] _bytes = ImageConversion.EncodeToPNG(newTexture);
                File.WriteAllBytes(filePath, _bytes);
            }
            
            AssetDatabase.ImportAsset("Assets/" + outputPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath("Assets/" + outputPath) as TextureImporter;
            if (importer != null)
            {
                if (saveExr)
                {
                    importer.textureType = TextureImporterType.Default;
                }
                else
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = false;
                }

                importer.wrapMode = TextureWrapMode.Clamp;

                importer.npotScale = TextureImporterNPOTScale.ToNearest;
                //importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.textureCompression = TextureImporterCompression.CompressedLQ;
                importer.isReadable = true;

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                AssetDatabase.ImportAsset("Assets/" + outputPath);
                AssetDatabase.Refresh();
            }

            //next read asset again
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/" + outputPath);
            Color[] tColors = t.GetPixels();
            for(int i = 0; i < tColors.Length; i++)
            {
                tColors[i] = tColors[i].linear;
            }
            Debug.Log(GetMaxDifference(tColors, lmColorsRaw));
#endif
        }
    }

}