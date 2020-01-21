using System.IO;
using Unity.Build.Common;
using UnityEditor;


namespace Unity.Entities.Runtime.Build
{
    static class MenuItemDRBS
    {
        const string kBuildSettingsDotsRT = "Assets/Create/Build/BuildSettings for DOTS Runtime";
        const string kBuildPipelineDotsRTAssetPath = "Packages/com.unity.dots.runtime/BuildPipelines/Default DOTS Runtime Pipeline.buildpipeline";

        [MenuItem(kBuildSettingsDotsRT, true)]
        static bool CreateNewBuildSettingsAssetValidationDotsRT()
        {
            return Directory.Exists(AssetDatabase.GetAssetPath(Selection.activeObject));
        }
        
        [MenuItem(kBuildSettingsDotsRT)]
        static void CreateNewBuildSettingsAssetDotsRT()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<Unity.Build.BuildPipeline>(kBuildPipelineDotsRTAssetPath);
            Selection.activeObject = MenuItemBuildSettings.CreateNewBuildSettingsAsset("DotsRT", new DotsRuntimeBuildProfile { Pipeline = pipeline });
        }
    }
}