using DOTS.Action;
using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Component.Trait;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using DOTS.System;
using DOTS.System.ActionExecuteSystem;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Entity = Unity.Entities.Entity;

namespace DOTS.Test
{
    public class TestNavigatingStartSystem : TestBase
    {
        private NavigatingStartSystem _system;
        private Entity _agentEntity, _containerEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<NavigatingStartSystem>();

            _agentEntity = EntityManager.CreateEntity();
            _containerEntity = EntityManager.CreateEntity();
            
            //container要有位置数据
            EntityManager.AddComponentData(_containerEntity, new Translation{Value = new float3(9,0,0)});
            
            EntityManager.AddComponentData(_agentEntity, new Agent{ExecutingNodeId = 0});
            EntityManager.AddComponentData(_agentEntity, new ReadyToNavigating());
            EntityManager.AddComponentData(_agentEntity, new PickRawAction());
            EntityManager.AddComponentData(_agentEntity, new Translation{Value = float3.zero});
            EntityManager.AddBuffer<ContainedItemRef>(_agentEntity);
            //agent必须带有已经规划好的任务列表
            var bufferNodes = EntityManager.AddBuffer<Node>(_agentEntity);
            bufferNodes.Add(new Node
            {
                NavigatingSubject = _containerEntity,
            });
        }
        
        //为agent赋予移动目标
        [Test]
        public void AddTargetPositionToAgent()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<TargetPosition>(_agentEntity));
            Assert.AreEqual(new float3(9,0,0),
                EntityManager.GetComponentData<TargetPosition>(_agentEntity).Value);
        }
        
        //切换agent状态
        [Test]
        public void NextAgentState()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<Navigating>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<ReadyToNavigating>(_agentEntity));
            Assert.Zero(EntityManager.GetComponentData<Agent>(_agentEntity).ExecutingNodeId);
        }
        
        //目标为自身则直接到达
        [Test]
        public void TargetIsSelf_ToNextState()
        {
            var buffer = EntityManager.GetBuffer<Node>(_agentEntity);
            buffer[0] = new Node
            {
                NavigatingSubject = _agentEntity,
            };
            
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<ReadyToActing>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<ReadyToNavigating>(_agentEntity));
            Assert.Zero(EntityManager.GetComponentData<Agent>(_agentEntity).ExecutingNodeId);
        }
        
    }
}