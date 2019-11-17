using DOTS.Struct;
using Unity.Entities;

namespace DOTS.Component
{
    /// <summary>
    /// 放在agent上，表示其正在planning的goal
    /// </summary>
    public struct PlanningGoal : IComponentData
    {
        public Node Goal;
    }
}