using System;
using DOTS.Component;
using DOTS.Component.Trait;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace DOTS.ActionJob
{
    
    [BurstCompile]
    public struct DropRawActionJob : IJobParallelForDefer
    {
        [ReadOnly]
        public NativeList<Entity> UnexpandedNodes;
        
        [ReadOnly]
        public BufferFromEntity<State> BuffersState;

        [ReadOnly]
        public StackData StackData;
        
        public EntityCommandBuffer.Concurrent ECBuffer;

        public DropRawActionJob(NativeList<Entity> unexpandedNodes,
            BufferFromEntity<State> buffersState, StackData stackData,
            EntityCommandBuffer.Concurrent ecBuffer)
        {
            UnexpandedNodes = unexpandedNodes;
            BuffersState = buffersState;
            StackData = stackData;
            ECBuffer = ecBuffer;
        }

        public void Execute(int jobIndex)
        {
            var unexpandedNode = UnexpandedNodes[jobIndex];
            var bufferStates = BuffersState[unexpandedNode];
            var targetStates = new StateGroup(ref bufferStates, Allocator.Temp);
            
            var preconditions = new StateGroup(1, Allocator.Temp);
            var effects = new StateGroup(1, Allocator.Temp);
            
            GetPreconditions(ref targetStates, ref StackData,
                ref preconditions);
            GetEffects(ref targetStates, ref StackData, ref effects);

            if (effects.Length() == 0) return;
            
            var newStates = new StateGroup(targetStates, Allocator.Temp);
            newStates.Sub(effects);
            newStates.Merge(preconditions);
            
            var nodeEntity = ECBuffer.CreateEntity(jobIndex);
            ECBuffer.AddComponent(jobIndex, nodeEntity, new Node{parent = unexpandedNode});
            //将变更后的states存入新node
            var buffer = ECBuffer.AddBuffer<State>(jobIndex, nodeEntity);
            newStates.WriteBuffer(ref buffer);
            
            newStates.Dispose();
            preconditions.Dispose();
            effects.Dispose();
            targetStates.Dispose();
        }
        
        /// <summary>
        /// 条件：自体要有对应物品
        /// </summary>
        /// <param name="targetStates"></param>
        /// <param name="stackData"></param>
        /// <param name="preconditions"></param>
        private void GetPreconditions([ReadOnly]ref StateGroup targetStates,
            [ReadOnly]ref StackData stackData, ref StateGroup preconditions)
        {
            foreach (var targetState in targetStates)
            {
                //只针对物品请求的goal state
                if (targetState.Trait != typeof(Inventory)) continue;

                var agent = stackData.AgentEntity;
                
                preconditions.Add(new State
                {
                    Target = agent,
                    Trait = typeof(Inventory),
                    Value = targetState.Value
                });

                //TODO 目前DropRaw只可以完成一项state，将来可以考虑做多重物品运送的同时满足
                break;
            }
        }

        /// <summary>
        /// 效果：目标获得对应物品
        /// </summary>
        /// <param name="targetStates"></param>
        /// <param name="stackData"></param>
        /// <param name="effects"></param>
        private void GetEffects([ReadOnly]ref StateGroup targetStates,
            [ReadOnly]ref StackData stackData, ref StateGroup effects)
        {
            foreach (var targetState in targetStates)
            {
                //只针对物品请求的goal state
                if (targetState.Trait != typeof(Inventory)) continue;

                effects.Add(new State
                {
                    Target = targetState.Target,
                    Trait = typeof(Inventory),
                    Value = targetState.Value
                });
                
                //TODO 目前DropRaw只可以完成一项state，将来可以考虑做多重物品运送的同时满足
                break;
            }
        }
    }
}