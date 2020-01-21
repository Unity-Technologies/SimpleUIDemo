using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Build;
using Unity.Build.Common;
using Unity.Scenes;
using Unity.Tiny;
#if TINY_SCENE_DEP
using Unity.Tiny.Scenes;
#endif
using UnityEditor;

namespace Unity.Entities.Runtime.Build
{
    [BuildStep(description = kDescription, category = "DOTS")]
    internal class BuildStepExportConfiguration : BuildStep
    {
        const string kDescription = "Export Configuration";

        public override string Description => kDescription;

        public override Type[] RequiredComponents => new[]
        {
            typeof(DotsRuntimeBuildProfile),
            typeof(SceneList)
        };

        public override Type[] OptionalComponents => new[]
        {
            typeof(OutputBuildDirectory)
        };

        public override BuildStepResult RunBuildStep(BuildContext context)
        {
#if TINY_SCENE_DEP
            var manifest = context.BuildManifest;
            var profile = GetRequiredComponent<DotsRuntimeBuildProfile>(context);
            var scenes = GetRequiredComponent<SceneList>(context);
            var firstScene = scenes.GetScenePathsForBuild().FirstOrDefault();

            using (var loadedSceneScope = new LoadedSceneScope(firstScene))
            {
                var projScene = loadedSceneScope.ProjectScene;

                var configGameObject = projScene.GetRootGameObjects()
                    .FirstOrDefault((go => go && go.name == "Configuration"));

                using (var tmpWorld = new World(ConfigurationScene.Guid.ToString("N")))
                {
                    //var configEntity = CopyEntity(context.WorldManager.GetConfigEntity(), context.World, tmpWorld);
                    var em = tmpWorld.EntityManager;

                    Entity configEntity;
                    if (configGameObject != null)
                    {
                        var exportDriver = new TinyExportDriver(context, profile.DataDirectory);
                        exportDriver.DestinationWorld = tmpWorld;
                        exportDriver.SceneGUID = new GUID(ConfigurationScene.Guid.ToString());

                        configEntity =
                            GameObjectConversionUtility.ConvertGameObjectHierarchy(configGameObject, exportDriver);
                        exportDriver.Write(manifest);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning(
                            $"Couldn't find GameObject in scene named 'Configuration' for DOTS RT/Tiny Config -- generating default");

                        configEntity = em.CreateEntity();
                        em.AddComponentData(configEntity, DisplayInfo.Default);
                    }

                    em.AddComponentData(configEntity, new ConfigurationTag());

#if EXPORT_TINY_SHADER
                    if (profile.TypeCache.HasType(typeof(Unity.Tiny.Rendering.PrecompiledShaders)))
                        TinyShader.Export(em, profile);
#endif

                    var subScenes = projScene.GetRootGameObjects()
                        .Select(go => go.GetComponent<SubScene>())
                        .Where(g => g != null && g);

                    var startupScenes = em.AddBuffer<StartupScenes>(configEntity);

                    // Add this root scene to StartupScenes
                    var projSceneGuid = new GUID(AssetDatabase.AssetPathToGUID(projScene.path));
#if false
                    startupScenes.Add(new StartupScenes()
                    {
                        SceneReference = new Unity.Tiny.Scenes.SceneReference()
                            {SceneGuid = new System.Guid(projSceneGuid.ToString())}
                    });
#endif
                    // Add all our subscenes with AutoLoadScene to StartupScenes
                    // (technically not necessary?)
                    var subSceneGuids = subScenes
                        .Where(s => s != null && s.SceneAsset != null && s.AutoLoadScene)
                        .Select(s => new System.Guid(s.SceneGUID.ToString()));
                    foreach (var guid in subSceneGuids)
                        startupScenes.Add(new StartupScenes()
                        { SceneReference = new Unity.Tiny.Scenes.SceneReference() { SceneGuid = guid } });

                    // Export configuration scene
                    var outputFile = profile.DataDirectory.GetFile(tmpWorld.Name);
                    context.GetOrCreateValue<WorldExportTypeTracker>()?.AddTypesFromWorld(tmpWorld);
                    WorldExport.WriteWorldToFile(tmpWorld, outputFile);

                    // Update manifest
                    manifest.Add(ConfigurationScene.Guid, ConfigurationScene.Path, outputFile.ToSingleEnumerable());

                    // Dump debug file
                    var debugFile =
                        new NPath(this.GetOutputBuildDirectory(context)).Combine("Logs/SceneExportLog.txt");
                    var debugAssets = manifest.Assets.OrderBy(x => x.Value)
                        .Select(x => $"{x.Key.ToString("N")} = {x.Value}").ToList();

                    var debugLines = new List<string>();

                    debugLines.Add("::Exported Assets::");
                    debugLines.AddRange(debugAssets);
                    debugLines.Add("\n");

                    // Write out a separate list of types that we see in the dest world
                    // as well as all types
                    for (int group = 0; group < 2; ++group)
                    {
                        IEnumerable<TypeManager.TypeInfo> typesToWrite;
                        if (group == 0)
                        {
                            var typeTracker = context.GetOrCreateValue<WorldExportTypeTracker>();
                            if (typeTracker == null)
                                continue;
                            typesToWrite = typeTracker.TypesInUse.Select(t =>
                                TypeManager.GetTypeInfo(TypeManager.GetTypeIndex(t)));
                            debugLines.Add($"::Exported Types (by stable hash)::");
                        }
                        else
                        {
                            typesToWrite = TypeManager.AllTypes;
                            debugLines.Add($"::All Types in TypeManager (by stable hash)::");
                        }

                        var debugTypeHashes = typesToWrite.OrderBy(ti => ti.StableTypeHash)
                            .Where(ti => ti.Type != null).Select(ti =>
                                $"0x{ti.StableTypeHash:x16} - {ti.StableTypeHash,22} - {ti.Type.FullName}");

                        debugLines.AddRange(debugTypeHashes);
                        debugLines.Add("\n");
                    }

                    debugFile.MakeAbsolute().WriteAllLines(debugLines.ToArray());
                }
            }
#endif
            return Success();
        }
    }
}
