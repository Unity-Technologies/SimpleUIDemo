using System;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace Unity.Tiny.Bootstrap.Hybrid
{
    public class TinyHybridWorldBootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            Debug.Log("Tiny Hybrid Bootstrap executing");
            var world = new World("Default World");
            World.DefaultGameObjectInjectionWorld = world;

            var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);

            // filter out the tiny systems
            systems = systems.Where(s => {
                var asmName = s.Assembly.FullName;
                if (asmName.Contains("Unity.Tiny") && !asmName.Contains("Hybrid"))
                {
                    return false;
                }

                return true;
            }).ToList();

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
            return true;
        }
    }
}
