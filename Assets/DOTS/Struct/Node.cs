using System;
using Unity.Collections;
using Unity.Entities;

namespace DOTS.Struct
{
    public struct Node : IEquatable<Node>, IPathFindingNode, IBufferElementData
    {
        public NativeString64 Name;
        
        /// <summary>
        /// 第一次展开到这个Node时的层数, goal=0
        /// </summary>
        private readonly int _iteration;

        /// <summary>
        /// -Cost/+Reward
        /// </summary>
        private readonly float _reward;
        
        /// <summary>
        /// 用于比较两个Node是否是同一个node
        /// 即只要两个Node的states全部一致即为同一个node
        /// </summary>
        private readonly int _hashCode;

        /// <summary>
        /// 当node被当做path存在agent上时，用bitmask指示其preconditions和effects对应的states in buffer
        /// </summary>
        public ulong PreconditionsBitmask;
        public ulong EffectsBitmask;

        /// <summary>
        /// 用于给Navigating指明导航目标
        /// </summary>
        public Entity NavigatingSubject;

        public Node(ref StateGroup states, NativeString64 name, float reward, int iteration, Entity navigatingSubject = new Entity()) : this()
        {
            Name = name;
            _reward = reward;
            _iteration = iteration;
            NavigatingSubject = navigatingSubject;
            _hashCode = states.GetHashCode();
        }
        
        public Node(ref State state, NativeString64 name, float reward, int iteration, Entity navigatingSubject = new Entity()) : this()
        {
            Name = name;
            _reward = reward;
            _iteration = iteration;
            NavigatingSubject = navigatingSubject;
            _hashCode = state.GetHashCode();
        }

        public bool Equals(Node other)
        {
            return _hashCode.Equals(other._hashCode);
        }

        public float GetReward([ReadOnly]ref NodeGraph nodeGraph)
        {
            return _reward;
        }

        public float Heuristic([ReadOnly]ref NodeGraph nodeGraph)
        {
            //todo 目前直接使用与goal的距离
            return _iteration - nodeGraph.GetGoalNode()._iteration;
        }

        public void GetNeighbours([ReadOnly]ref NodeGraph nodeGraph, ref NativeList<int> neighboursId)
        {
            //所有的parent即为neighbour
            var edges = nodeGraph.GetEdgeToParents(this);
            foreach (var edge in edges)
            {
                neighboursId.Add(edge.Parent._hashCode);
            }
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}