using Unity.Entities;

namespace Zephyr.GOAP.Component.GoalManage.GoalState
{
    /// <summary>
    /// 表示一个goal正在被一个agent规划中
    /// </summary>
    public struct PlanningGoal : IComponentData, IGoalState
    {
        public Entity AgentEntity { get; set; }
        public float Time { get; set; }
    }
}