using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using Unity.Platforms;
using Unity.Properties.Editor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Entities.Runtime.Build
{
    [UsedImplicitly]
    sealed class BuildTargetInspector : IInspector<BuildTarget>
    {
        PopupField<BuildTarget> m_TargetPopup;

        public VisualElement Build(InspectorContext<BuildTarget> context)
        {
            m_TargetPopup = new PopupField<BuildTarget>(GetAvailableTargets(), 0, GetDisplayName, GetDisplayName)
            {
                label = context.PrettyName
            };

            m_TargetPopup.RegisterValueChangedCallback(evt =>
            {
                context.Data = evt.newValue;
            });
            return m_TargetPopup;
        }

        public void Update(InspectorContext<BuildTarget> proxy)
        {
            m_TargetPopup.SetValueWithoutNotify(proxy.Data);
        }

        static List<BuildTarget> GetAvailableTargets() => BuildTarget.AvailableBuildTargets.Where(target => !target.HideInBuildTargetPopup).ToList();
        static string GetDisplayName(BuildTarget target) => target.DisplayName;
    }
}
