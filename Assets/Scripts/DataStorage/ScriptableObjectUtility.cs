using UnityEngine;
using UnityEditor;
using System.IO;

namespace WorldGenerator
{
	public static class ScriptableObjectUtility
	{
		/// <summary>
		//	This makes it easy to create, name and place unique new ScriptableObject asset files.
		/// </summary>
		public static T CreateAsset<T>(string path, string assetName) where T : ScriptableObject
		{
			T asset = ScriptableObject.CreateInstance<T>();

			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + assetName + ".asset");

			AssetDatabase.CreateAsset(asset, assetPathAndName);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			//EditorUtility.FocusProjectWindow();
			//Selection.activeObject = asset;

			return asset;
		}
	}

}