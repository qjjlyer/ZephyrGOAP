using Unity.Entities;

namespace DOTS.Component
{
    public struct Node : IComponentData
    {
        public Entity parent;
    }
}