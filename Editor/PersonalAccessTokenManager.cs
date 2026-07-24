using UnityEditor;

namespace com.parkminpackages.packagemanager.Editor
{
	internal class PersonalAccessTokenManager
	{
		public static void SaveToken(string token) {
			EditorPrefs.SetString("MutantPackageManager.Token", token);
		}
		public static string LoadToken() {
			return EditorPrefs.GetString("MutantPackageManager.Token");
		}
	}
}