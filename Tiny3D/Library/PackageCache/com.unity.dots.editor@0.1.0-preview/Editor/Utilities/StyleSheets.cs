using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Editor
{
    static class StyleSheets
    {
        public readonly struct UxmlTemplate
        {
            private readonly string UxmlPath;
            private readonly string UssPath;

            public UxmlTemplate(string name)
            {
                UxmlPath = k_UxmlBasePath + name + ".uxml";
                UssPath = k_UssBasePath + name + ".uss";
            }

            public VisualTreeAsset Template => EditorGUIUtility.Load(UxmlPath) as VisualTreeAsset;
            public StyleSheet StyleSheet => AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
        }

        const string k_BasePath = Constants.EditorDefaultResourcesPath;
        const string k_UssBasePath = k_BasePath + "uss/";
        const string k_UxmlBasePath = k_BasePath + "uxml/";
    }
}