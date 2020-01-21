using System.Collections.Generic;
using Unity.Build;
using Unity.Properties;

namespace Unity.Entities.Runtime.Build
{
    public class DotsRuntimeScriptingDefines : IBuildSettingsComponent
    {
        [Property] 
        public List<string> ScriptingDefines { get; set; } = new List<string>();
    }
}