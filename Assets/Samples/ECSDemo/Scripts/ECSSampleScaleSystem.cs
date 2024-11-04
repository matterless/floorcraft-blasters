using System;
using System.Collections.Generic;
using Auki.ConjureKit;
using Auki.ConjureKit.ECS;
using UnityEngine;

namespace AukiSampleECSDemo
{
    /// <summary>
    /// Sample system inheriting from ConjureKit's SystemBase that will add a component with type name "test_scale" to an entity
    /// and update its value when the UpdateLocalEntityScale() method is called.
    /// </summary>
    public class ECSSampleScaleSystem : SystemBase
    {
        private const string TEST_SCALE_COMPONENT_NAME = "test_scale";
        private readonly Action<uint, float> _onNewScaleDataReceived;
        
        public ECSSampleScaleSystem(Action<uint, float> onNewScaleDataReceived, Session session) : base(session)
        {
            _onNewScaleDataReceived = onNewScaleDataReceived;
        }

        public override string[] GetComponentTypeNames()
        {
            return new[] {TEST_SCALE_COMPONENT_NAME};
        }

        public override void Update(IReadOnlyList<(EntityComponent component, bool localChange)> updated)
        {
            foreach (var entityComponent in updated)
            {
                var floatArray = new float[1];
                Buffer.BlockCopy(entityComponent.component.Data, 0, floatArray, 0, 1 * sizeof(float));
                _onNewScaleDataReceived(entityComponent.component.EntityId, floatArray[0]);
            }
        }

        public override void Delete(IReadOnlyList<(EntityComponent component, bool localChange)> deleted)
        {
        }

        /// <summary>
        /// Adds the scale component to an entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="onComplete"></param>
        public void AttachToEntity(Entity entity, Action onComplete)
        {
            var scaleArray = new[] {0.1f};
            var byteArray = new byte[sizeof(float)];
            Buffer.BlockCopy(scaleArray, 0, byteArray, 0, 1 * sizeof(float));

            _session.AddComponent(
                TEST_SCALE_COMPONENT_NAME,
                entity.Id,
                byteArray,
                onComplete,
                e => Debug.LogError($"AddComponent failed: {e}")
            );
        }

        /// <summary>
        /// Calculates a new scale & updates the component value.
        /// </summary>
        /// <param name="entity">Entity that will have its scale component updated</param>
        public void UpdateLocalEntityScale(Entity entity)
        {
            if (entity == null) return;
    
            var scale = (Time.time % 2 + 1) * 0.01f;
            var scaleArray = new[] {scale};
            var byteArray = new byte[sizeof(float)];
            Buffer.BlockCopy(scaleArray, 0, byteArray, 0, 1 * sizeof(float));
            _session.UpdateComponent(TEST_SCALE_COMPONENT_NAME, entity.Id, byteArray);
            
            // Update local game object since the broadcasts are only for remote participants.
            _onNewScaleDataReceived(entity.Id, scale);
        }
    }
}