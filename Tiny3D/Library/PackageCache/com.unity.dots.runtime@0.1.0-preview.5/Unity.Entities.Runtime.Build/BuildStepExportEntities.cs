using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Build;
using Unity.Build.Common;
using Unity.Scenes;
using Unity.Scenes.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Entities.Runtime.Build
{
    [BuildStep(description = k_Description, category = "DOTS")]
    internal class BuildStepExportEntities : BuildStep
    {
        const string k_Description = "Export Entities";

        public override string Description => k_Description;

        public override Type[] RequiredComponents => new[]
        {
            typeof(DotsRuntimeBuildProfile),
            typeof(SceneList)
        };

        public override BuildStepResult RunBuildStep(BuildContext context)
        {
            var manifest = context.BuildManifest;
            var settings = GetRequiredComponent<DotsRuntimeBuildProfile>(context);
            var buildScenes = GetRequiredComponent<SceneList>(context);

            var exportedSceneGuids = new HashSet<Guid>();

            var originalActiveScene = SceneManager.GetActiveScene();

            void ExportSceneToFile(Scene scene, Guid guid)
            {
                var outputFile = settings.DataDirectory.GetFile(guid.ToString("N"));
                using (var exportWorld = new World("Export World"))
                {
                    var exportDriver = new TinyExportDriver(context, settings.DataDirectory);
                    exportDriver.DestinationWorld = exportWorld;
                    exportDriver.SceneGUID = new Hash128(guid.ToString("N"));

                    SceneManager.SetActiveScene(scene);

                    GameObjectConversionUtility.ConvertScene(scene, exportDriver);
                    context.GetOrCreateValue<WorldExportTypeTracker>()?.AddTypesFromWorld(exportWorld);

#if EXPORT_TINY_SHADER
                    RenderSettingsConversion.ConvertRenderSettings(exportWorld.EntityManager);
#endif

                    WorldExport.WriteWorldToFile(exportWorld, outputFile);
                    exportDriver.Write(manifest);
                }

                manifest.Add(guid, scene.path, outputFile.ToSingleEnumerable());
            }

            foreach (var rootScenePath in buildScenes.GetScenePathsForBuild())
            {
                using (var loadedSceneScope = new LoadedSceneScope(rootScenePath))
                {
                    var thisSceneGuid = new Guid(AssetDatabase.AssetPathToGUID(rootScenePath));
                    if (exportedSceneGuids.Contains(thisSceneGuid))
                        continue;

                    ExportSceneToFile(loadedSceneScope.ProjectScene, thisSceneGuid);
                    exportedSceneGuids.Add(thisSceneGuid);

                    var thisSceneSubScenes = loadedSceneScope.ProjectScene.GetRootGameObjects()
                        .Select(go => go.GetComponent<SubScene>())
                        .Where(g => g != null && g);

                    foreach (var subScene in thisSceneSubScenes)
                    {
                        var guid = new Guid(subScene.SceneGUID.ToString());
                        if (exportedSceneGuids.Contains(guid))
                            continue;

                        var isLoaded = subScene.IsLoaded;
                        if (!isLoaded)
                            SubSceneInspectorUtility.EditScene(subScene);

                        var scene = subScene.EditingScene;
                        var sceneGuid = subScene.SceneGUID;

                        ExportSceneToFile(scene, guid);

                        if (!isLoaded)
                            SubSceneInspectorUtility.CloseSceneWithoutSaving(subScene);
                    }
                }
            }

            SceneManager.SetActiveScene(originalActiveScene);

            return Success();
        }
    }
}
