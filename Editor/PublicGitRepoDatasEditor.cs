using System.IO;
using UnityEditor;
using UnityEngine;

namespace ParkMinPackages.PackageManager.Editor
{
	[CustomEditor(typeof(PublicGitRepoDatas))]
	internal class PublicGitRepoDatasEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI() {
			serializedObject.Update();
			DrawDefaultInspector();
			GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

			if (GUILayout.Button("Create Public Git Repo Data")) {
				CreatePublicGitRepoData();
			}

			serializedObject.ApplyModifiedProperties();
		}

		void CreatePublicGitRepoData() {
			PublicGitRepoDatas publicGitRepoDatas = (PublicGitRepoDatas)target;
			string collectionAssetPath = AssetDatabase.GetAssetPath(publicGitRepoDatas);
			string collectionFolderPath = Path.GetDirectoryName(collectionAssetPath)?.Replace('\\', '/');
			if (string.IsNullOrEmpty(collectionFolderPath)) {
				Debug.LogError($"{nameof(PublicGitRepoDatas)} must be saved as an asset before creating data.");
				return;
			}

			string dataFolderPath = $"{collectionFolderPath}/Datas";
			if (!AssetDatabase.IsValidFolder(dataFolderPath)) {
				AssetDatabase.CreateFolder(collectionFolderPath, "Datas");
			}

			PublicGitRepoData publicGitRepoData = CreateInstance<PublicGitRepoData>();
			string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{dataFolderPath}/{nameof(PublicGitRepoData)}.asset");
			AssetDatabase.CreateAsset(publicGitRepoData, assetPath);
			Undo.RegisterCreatedObjectUndo(publicGitRepoData, $"Create {nameof(PublicGitRepoData)}");

			serializedObject.Update();
			SerializedProperty valueProperty = serializedObject.FindProperty("_value");
			int newIndex = valueProperty.arraySize;
			valueProperty.InsertArrayElementAtIndex(newIndex);
			valueProperty.GetArrayElementAtIndex(newIndex).objectReferenceValue = publicGitRepoData;
			serializedObject.ApplyModifiedProperties();

			EditorUtility.SetDirty(publicGitRepoDatas);
			AssetDatabase.SaveAssets();
			Selection.activeObject = publicGitRepoData;
			EditorGUIUtility.PingObject(publicGitRepoData);
		}
	}
}
