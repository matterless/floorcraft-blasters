using UnityEngine;

namespace Matterless.Floorcraft
{
    public interface INPCEnemySimulation
    {
        EnemyState state { get; }
        Vector3 groundPosition { get; }
        Vector3 floorNormal { get; }
        float speed { get; }
        uint entityId { get; }
        Quaternion rotation { get; }
        void Init(NpcEnemyInputModel inputModel, Vector3 target);
        public NpcEnemyInputModel Update(float deltaTime, NpcEnemyInputModel inputModel);
    }
}