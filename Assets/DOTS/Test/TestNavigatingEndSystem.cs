using DOTS.Component;
using DOTS.Component.AgentState;
using DOTS.Game.ComponentData;
using DOTS.Struct;
using DOTS.System;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Test
{
    public class TestNavigatingEndSystem : TestBase
    {
        private NavigatingEndSystem _system;
        private Entity _agentEntity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            _system = World.GetOrCreateSystem<NavigatingEndSystem>();

            _agentEntity = EntityManager.CreateEntity();
            
            EntityManager.AddComponentData(_agentEntity, new Agent{ExecutingNodeId = 0});
            EntityManager.AddComponentData(_agentEntity, new Navigating());
            EntityManager.AddComponentData(_agentEntity, new TargetPosition{Value = float3.zero});
            //agent必须带有已经规划好的任务列表
            var bufferNodes = EntityManager.AddBuffer<Node>(_agentEntity);
            bufferNodes.Add(new Node());
        }
        
        //改变agent状态，不改变NodeId
        [Test]
        public void NextAgentState()
        {
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsTrue(EntityManager.HasComponent<ReadyToActing>(_agentEntity));
            Assert.IsFalse(EntityManager.HasComponent<Navigating>(_agentEntity));
            Assert.Zero(EntityManager.GetComponentData<Agent>(_agentEntity).ExecutingNodeId);
        }
        
        //未移动完毕，继续等待
        [Test]
        public void WaitForMovingDone()
        {
            EntityManager.SetComponentData(_agentEntity, new TargetPosition{Value = new float3(9,0,0)});
            
            _system.Update();
            _system.ECBSystem.Update();
            EntityManager.CompleteAllJobs();
            
            Assert.IsFalse(EntityManager.HasComponent<ReadyToActing>(_agentEntity));
            Assert.IsTrue(EntityManager.HasComponent<Navigating>(_agentEntity));
            Assert.Zero(EntityManager.GetComponentData<Agent>(_agentEntity).ExecutingNodeId);
        }
    }
}