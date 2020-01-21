using Unity.Build;

namespace Unity.Entities.Runtime.Build
{
    internal class DotsRuntimeRunInstance : IRunInstance
    {
        public bool IsRunning => throw new System.NotImplementedException();

        public void Dispose()
        {
        }
    }
}
