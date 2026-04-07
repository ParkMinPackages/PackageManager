using UnityEditor;
using UnityEngine;

namespace com.mutant.packagemanager.Editor
{
    public class Menu
    {
        [MenuItem("Mutant/PackageManager")]
        static void Execute()
        {
            PackageManagerWindow window = EditorWindow.GetWindow<PackageManagerWindow>();
            window.titleContent = new GUIContent("Mutant Package Manager");
            window.minSize = new Vector2(500, 300);
            window.maxSize = new Vector2(500, 10000); 
        }
    }
}
