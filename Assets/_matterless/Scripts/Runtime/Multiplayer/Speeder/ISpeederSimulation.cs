using UnityEngine;

namespace Matterless.Floorcraft
{
    public interface ISpeederSimulation
    {
        float boosting { get; }
        float braking { get; }
        SpeederState state { get; }
        float age { get; }
        Vector3 groundPosition { get; }
        Vector3 floorNormal { get; }
        float speed { get; }
        float brakingLength { get; }
        uint entityId { get; }
        Quaternion rotation { get; }
        bool isPlayer { get; }
        SpeederGameplayModel lastSpeederGameModel { get; set; }
        bool crownKeeper { get; }
        void Init(SpeederInputModel inputModel);
        void Update(float deltaTime, SpeederInputModel inputModel);
    }
}