using System;
using System.Collections.Generic;
using System.IO;
using Unity.Build;
using Unity.Properties;
using Unity.Serialization;
using Unity.Serialization.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using BuildPipeline = Unity.Build.BuildPipeline;
using BuildTarget = Unity.Platforms.BuildTarget;
using PropertyAttribute = Unity.Properties.PropertyAttribute;

namespace Unity.Entities.Runtime.Build
{
    public sealed class DotsRuntimeBuildProfile : IBuildPipelineComponent
    {
        BuildTarget m_Target;
        List<string> m_ExcludedAssemblies;

        /// <summary>
        /// Retrieve <see cref="BuildTypeCache"/> for this build profile.
        /// </summary>
        public BuildTypeCache TypeCache { get; } = new BuildTypeCache();

        [Property]
        [HideInInspector]
        public bool EnableManagedDebugging { get; set; } = false;
        [Property]
        [HideInInspector]
        public bool EnableMultiThreading { get; set; } = false;
        [Property]
        public bool EnableBurst { get; set; } = true;

        /// <summary>
        /// Gets or sets the root assembly for this DOTS Runtime build.  This root
        /// assembly determines what other assemblies will be pulled in for the build.
        /// </summary>
        [Property]
        public AssemblyDefinitionAsset RootAssembly
        {
            get { return m_RootAssembly; }
            set
            {
                m_RootAssembly = value;
                TypeCache.BaseAssemblies = new[] { m_RootAssembly };
            }
        }

        AssemblyDefinitionAsset m_RootAssembly;

        /// <summary>
        /// Gets or sets which <see cref="Platforms.BuildTarget"/> this profile is going to use for the build.
        /// Used for building Dots Runtime players.
        /// </summary>
        [Property]
        public BuildTarget Target
        {
            get => m_Target;
            set
            {
                m_Target = value;
                TypeCache.PlatformName = m_Target?.UnityPlatformName;
            }
        }

        /// <summary>
        /// Gets or sets which <see cref="Configuration"/> this profile is going to use for the build.
        /// </summary>
        [Property]
        public BuildConfiguration Configuration { get; set; } = BuildConfiguration.Develop;

        [Property]
        public BuildPipeline Pipeline { get; set; }

        public string ProjectName
        {
            get
            {
                if (RootAssembly == null || !RootAssembly)
                    return null;
                // FIXME should maybe be RootAssembly.name, but this is super confusing
                var asmdefPath = AssetDatabase.GetAssetPath(RootAssembly);
                var asmdefFilename = Path.GetFileNameWithoutExtension(asmdefPath);

                // just require that they're identical for this root assembly
                if (!asmdefFilename.Equals(RootAssembly.name))
                {
                    throw new InvalidOperationException($"Root asmdef {asmdefPath} must have its assembly name (currently '{RootAssembly.name}') set to the same as the filename (currently '{asmdefFilename}')");
                }

                return asmdefFilename;
            }
        }

        public string BeeTargetName
        {
            get { return BeeTargetOverride ?? $"{ProjectName}-{Target.BeeTargetName}-{Configuration.ToString()}".ToLower(); }
        }

        [Property, HideInInspector]
        public string BeeTargetOverride { get; set; }

        // FIXME
        public bool ShouldWriteDataFiles = true;

        public DirectoryInfo BeeRootDirectory => new DirectoryInfo("Library/DotsRuntimeBuild");
        public DirectoryInfo StagingDirectory => new DirectoryInfo($"Library/DotsRuntimeBuild/{ProjectName}");
        
        public DirectoryInfo DataDirectory => new DirectoryInfo($"Library/DotsRuntimeBuild/{ProjectName}/Data");

        /// <summary>
        /// List of assemblies that should be explicitly excluded for the build.
        /// </summary>
        //[Property]
        public List<string> ExcludedAssemblies
        {
            get => m_ExcludedAssemblies;
            set
            {
                m_ExcludedAssemblies = value;
                TypeCache.ExcludedAssemblies = value;
            }
        }

        public DotsRuntimeBuildProfile()
        {
            Target = BuildTarget.DefaultBuildTarget;
            ExcludedAssemblies = new List<string>();
        }

        class DotsRuntimeBuildProfileJsonAdapter : JsonVisitorAdapter,
            IVisitAdapter<BuildTarget>
        {
            [InitializeOnLoadMethod]
            static void Initialize()
            {
                BuildSettings.JsonVisitorRegistration += (JsonVisitor visitor) =>
                {
                    visitor.AddAdapter(new DotsRuntimeBuildProfileJsonAdapter(visitor));
                };

                TypeConversion.Register<SerializedStringView, BuildTarget>((view) =>
                {
                    return BuildTarget.GetBuildTargetFromBeeTargetName(view.ToString());
                });
            }

            public DotsRuntimeBuildProfileJsonAdapter(JsonVisitor visitor) : base(visitor) { }

            public VisitStatus Visit<TProperty, TContainer>(IPropertyVisitor visitor, TProperty property, ref TContainer container, ref BuildTarget value, ref ChangeTracker changeTracker)
                where TProperty : IProperty<TContainer, BuildTarget>
            {
                AppendJsonString(property, value?.BeeTargetName);
                return VisitStatus.Handled;
            }
        }
    }
}
