using Unity.Entities;

namespace Zephyr.GOAP.Component
{
    public struct Agent : IComponentData
    {
        public int ExecutingNodeId;
    }
}