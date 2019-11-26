using System;
using DOTS.Component;
using DOTS.Component.Trait;
using DOTS.Struct;
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
        public NativeList<Node> UnexpandedNodes;

        [ReadOnly]
        public StackData StackData;
        
        public NodeGraph NodeGraph;

        [NativeDisableParallelForRestriction]
        public NativeList<Node> NewlyExpandedNodes;

        public DropRawActionJob(ref NativeList<Node> unexpandedNodes, ref StackData stackData,
            ref NodeGraph nodeGraph, ref NativeList<Node> newlyExpandedNodes)
        {
            UnexpandedNodes = unexpandedNodes;
            StackData = stackData;
            NodeGraph = nodeGraph;
            NewlyExpandedNodes = newlyExpandedNodes;
        }

        public void Execute(int jobIndex)
        {
            var unexpandedNode = UnexpandedNodes[jobIndex];
            var targetStates = NodeGraph.GetStateGroup(unexpandedNode, Allocator.Temp);
            
            var preconditions = new StateGroup(1, Allocator.Temp);
            var effects = new StateGroup(1, Allocator.Temp);
            
            var targetState = GetTargetGoalState(ref targetStates, ref StackData);
            
            if (!targetState.Equals(default))
            {
                GetPreconditions(ref targetState, ref StackData,
                    ref preconditions);
                GetEffects(ref targetState, ref StackData, ref effects);

                if (effects.Length() == 0) return;
            
                var newStates = new StateGroup(targetStates, Allocator.Temp);
                newStates.Sub(effects);
                newStates.Merge(preconditions);
            
                var node = new Node(ref newStates, "DropRaw");
            
                //NodeGraph的几个容器都移去了并行限制，小心出错
                NodeGraph.AddRouteNode(node, ref newStates, unexpandedNode,
                    new NativeString64("DropRaw"));
                NewlyExpandedNodes.Add(node);
                
                newStates.Dispose();
            }
            
            preconditions.Dispose();
            effects.Dispose();
            targetStates.Dispose();
        }
        
        private State GetTargetGoalState([ReadOnly]ref StateGroup targetStates,
            [ReadOnly]ref StackData stackData)
        {
            foreach (var targetState in targetStates)
            {
                //只针对非自身目标的原料请求的goal state
                if (targetState.Target == stackData.AgentEntity) continue;
                if (targetState.Trait != typeof(RawTrait)) continue;

                return targetState;
            }

            return default;
        }
        
        /// <summary>
        /// 条件：自体要有对应物品
        /// </summary>
        /// <param name="targetState"></param>
        /// <param name="stackData"></param>
        /// <param name="preconditions"></param>
        private void GetPreconditions([ReadOnly]ref State targetState,
            [ReadOnly]ref StackData stackData, ref StateGroup preconditions)
        {
            preconditions.Add(new State
            {
                SubjectType = StateSubjectType.Self,
                Target = stackData.AgentEntity,
                Trait = typeof(RawTrait),
                Value = targetState.Value,
                IsPositive = true
            });
        }

        /// <summary>
        /// 效果：目标获得对应物品
        /// </summary>
        /// <param name="targetState"></param>
        /// <param name="stackData"></param>
        /// <param name="effects"></param>
        private void GetEffects([ReadOnly]ref State targetState,
            [ReadOnly]ref StackData stackData, ref StateGroup effects)
        {
            effects.Add(new State
            {
                SubjectType = StateSubjectType.Target,
                Target = targetState.Target,
                Trait = typeof(RawTrait),
                Value = targetState.Value,
                IsPositive = true,
            });
                
            //TODO 目前DropRaw只可以完成一项state，将来可以考虑做多重物品运送的同时满足
        }
    }
}