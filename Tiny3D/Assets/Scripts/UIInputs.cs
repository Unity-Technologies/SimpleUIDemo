using Unity.Entities;

namespace Tiny3D
{
    [GenerateAuthoringComponent]
    public struct UIInputs : IComponentData
    {
        public float HorizontalAxis;
        public float VertAxis;
    }
}