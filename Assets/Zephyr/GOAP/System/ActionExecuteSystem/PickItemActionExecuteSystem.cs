using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Assertions;
using Zephyr.GOAP.Action;
using Zephyr.GOAP.Component;
using Zephyr.GOAP.Component.AgentState;
using Zephyr.GOAP.Game.ComponentData;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.ActionExecuteSystem
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class PickItemActionExecuteSystem : JobComponentSystem
    {
        public EntityCommandBufferSystem ECBSystem;
            
        protected override void OnCreate()
        {
            base.OnCreate();
            ECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        // [BurstCompile]
        [RequireComponentTag(typeof(PickItemAction), typeof(ContainedItemRef), typeof(ReadyToAct))]
        public struct ActionExecuteJob : IJobForEachWithEntity_EBBC<Node, State, Agent>
        {
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<ContainedItemRef> AllContainedItemRefs;
            
            public EntityCommandBuffer.Concurrent ECBuffer;
            
            public void Execute(Entity entity, int jobIndex, DynamicBuffer<Node> nodes,
                DynamicBuffer<State> states, ref Agent agent)
            {
                //执行进度要处于正确的id上
                var currentNode = nodes[agent.ExecutingNodeId];
                if (!currentNode.Name.Equals(new NativeString64(nameof(PickItemAction))))
                    return;
                
                //从precondition里找目标.
                var targetEntity = Entity.Null;
                var targetItemName = new NativeString64();
                for (var i = 0; i < states.Length; i++)
                {
                    if ((currentNode.PreconditionsBitmask & (ulong)1 << i) > 0)
                    {
                        var precondition = states[i];
                        Assert.IsTrue(precondition.Target!=null);
                        
                        targetEntity = precondition.Target;
                        targetItemName = precondition.ValueString;
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
                Utils.NextAgentState<ReadyToAct, ReadyToNavigate>(
                    entity, jobIndex, ref ECBuffer, agent, true);
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new ActionExecuteJob
            {
                AllContainedItemRefs = GetBufferFromEntity<ContainedItemRef>(),
                ECBuffer = ECBSystem.CreateCommandBuffer().ToConcurrent()
            };
            var handle = job.Schedule(this, inputDeps);
            ECBSystem.AddJobHandleForProducer(handle);
            return handle;
        }
    }
}