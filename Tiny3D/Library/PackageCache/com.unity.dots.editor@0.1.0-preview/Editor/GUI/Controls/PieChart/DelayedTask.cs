using System;
using UnityEngine.UIElements;

namespace Unity.Editor
{
    /// <summary>
    /// Task action will only be executed once at the beginning of the next frame,
    /// despite the number of the <see cref="Execute"/> methods calls.
    /// </summary>
    public class DelayedTask
    {
        private readonly IVisualElementScheduledItem m_ScheduledTask;
        
        public DelayedTask(VisualElement visualElement, Action action)
        {
            m_ScheduledTask = visualElement.schedule.Execute(action);
            m_ScheduledTask.Pause();
        }

        public void Execute()
        {
            if (!m_ScheduledTask.isActive)
            {
                m_ScheduledTask.Resume();
            }
        }
    }
}