using UnityEditor;
using UnityEngine;

namespace ParkMinPackages.PackageManager.Editor
{
	internal class Menu
	{
		[MenuItem("ParkMinPackages/Package Manager")]
		static void Execute() {
			PackageManagerWindow window = EditorWindow.GetWindow<PackageManagerWindow>();
			window.titleContent = new GUIContent("ParkMinPackages Package Manager");
			window.minSize = new Vector2(500, 300);
			window.maxSize = new Vector2(500, 10000);
		}
	}
}