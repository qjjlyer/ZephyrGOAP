using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Assertions;

namespace DOTS.System.ActionExecuteSystem
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class PickRawActionExecuteSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem ECBSystem;
            
        protected override void OnCreate()
        {
            base.OnCreate();
            ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        [RequireComponentTag(typeof(PickRawAction), typeof(ContainedItemRef), typeof(ReadyToActing))]
        public struct PickRawActionExecuteJob : IJobForEachWithEntity_EBBC<Node, State, Agent>
        {
            [ReadOnly]
            public StateGroup CurrentStates;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity<ContainedItemRef> AllContainedItemRefs;
            
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<Node> nodes,
                DynamicBuffer<State> states, ref Agent agent)
            {
                //执行进度要处于正确的id上
                var currentNode = nodes[agent.ExecutingNodeId];
                if (!currentNode.Name.Equals(new NativeString64(nameof(PickRawAction))))
                    return;
                
                //从precondition里找目标.
                var targetEntity = Entity.Null;
                var targetItemName = new NativeString64();
                for (var i = 0; i < states.Length; i++)
                {
                    if ((currentNode.PreconditionsBitmask & (ulong)1 << i) > 0)
                    {
                        var precondition = states[i];
                        Assert.IsTrue(precondition.Target!=null ||
                                      precondition.SubjectType == StateSubjectType.Closest);
                        
                        if (precondition.Target != Entity.Null)
                        {
                            targetEntity = precondition.Target;
                        }
                        else if(precondition.SubjectType == StateSubjectType.Closest)
                        {
                            //如果SubjectType为Closest，从CurrentState里找最近的目标
                            //todo 此处理应寻找最近目标，但目前的示例里没有transform系统，暂时直接用第一个合适的目标
                            foreach (var currentState in CurrentStates)
                            {
                                if (currentState.Fits(precondition))
                                {
                                    targetEntity = currentState.Target;
                                    break;
                                }
                            }
                        }
                        targetItemName = precondition.Value;
                        break;
                    }
                }
                //从目标身上找到物品引用，并移除
                var itemRef = new ContainedItemRef();
                var id = 0;
                var bufferContainedItems = AllContainedItemRefs[targetEntity];
                for (var i = 0; i < bufferContainedItems.Length; i++)
                {
                    var containedItemRef = bufferContainedItems[i];
                    if (!containedItemRef.ItemName.Equals(targetItemName)) continue;
                    
                    itemRef = containedItemRef;
                    id = i;
                    break;
                }
                bufferContainedItems.RemoveAt(id);

                //自己获得物品
                var buffer = AllContainedItemRefs[entity];
                buffer.Add(itemRef);
                
                //通知执行完毕
                Utils.NextAgentState<ReadyToActing, ReadyToNavigating>(
                    entity, jobIndex, ref ECBuffer, agent, true);
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var currentStates =
                CurrentStatesHelper.GetCurrentStates(EntityManager, Allocator.TempJob);
            var job = new PickRawActionExecuteJob
            {
                CurrentStates = currentStates,
                AllContainedItemRefs = GetBufferFromEntity<ContainedItemRef>(),
                ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent()
            };
            var handle = job.Schedule(this, inputDeps);
            ECBSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}