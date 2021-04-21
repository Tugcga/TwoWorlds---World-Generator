using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WorldGenerator
{
	public static class ScriptableObjectUtility
	{
#if UNITY_EDITOR
		public static T CreateAsset<T>(string path, string assetName) where T : ScriptableObject
		{
			T asset = ScriptableObject.CreateInstance<T>();

			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + assetName + ".asset");

			AssetDatabase.CreateAsset(asset, assetPathAndName);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			return asset;
		}
#endif
	}
}