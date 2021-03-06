using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Zephyr.GOAP.Component.Trait;
using Zephyr.GOAP.Struct;

namespace Zephyr.GOAP.System.SensorSystem
{
    /// <summary>
    /// 检测世界里的DiningTable，写入其存在
    /// </summary>
    [UpdateInGroup(typeof(SensorSystemGroup))]
    public class DiningTableSensorSystem : JobComponentSystem
    {
        [RequireComponentTag(typeof(DiningTableTrait))]
        private struct SenseJob : IJobForEachWithEntity_EC<Translation>
        {
            [NativeDisableContainerSafetyRestriction, WriteOnly]
            public BufferFromEntity<State> States;

            public Entity CurrentStatesEntity;
            
            public void Execute(Entity entity, int jobIndex, ref Translation translation)
            {
                //写入diningTable
                var buffer = States[CurrentStatesEntity];
                buffer.Add(new State
                {
                    Target = entity,
                    Position = translation.Value,
                    Trait = typeof(DiningTableTrait),
                });
            }
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new SenseJob
            {
                States = GetBufferFromEntity<State>(),
                CurrentStatesEntity = CurrentStatesHelper.CurrentStatesEntity
            };
            var handle = job.Schedule(this, inputDeps);
            return handle;
        }
    }
}