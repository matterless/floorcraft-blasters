using UnityEngine;

namespace Matterless.Floorcraft
{
    public class EnemyMapper
    {
        public static EnemyViewModel ToViewModel(INPCEnemySimulation simulation) =>
            new EnemyViewModel
            (
                simulation.entityId,
                simulation.groundPosition,
                simulation.rotation,
                simulation.state,
                simulation.speed
                );

        public static NpcEnemyInputModel ToInputModel(Pose pose, float speed = 0f) =>
            ToInputModel(pose.position, pose.rotation, speed);

        public static NpcEnemyInputModel ToInputModel(
            Vector3 position,
            Quaternion rotation,
            float speed = 0f)
        {
            var inputModel = new NpcEnemyInputModel();
            inputModel.position = position;
            inputModel.rotation = rotation;
            inputModel.speed = speed;
            return inputModel;
        }

        public static EnemyGameModel ToGameModel(INPCEnemySimulation simulation) =>
            new EnemyGameModel
            (
                simulation.state,
                simulation.groundPosition,
                simulation.rotation,
                simulation.speed);
        
        public static NpcEnemyInputModel ToInputModel(TransformModel transform,
            Vector3 position,
            bool isPlayer)
        {
            var inputModel = new NpcEnemyInputModel();
            if (isPlayer)
            {
                inputModel.position = position;
            }
            else
            {
                inputModel.position = transform.position;
                inputModel.rotation = transform.rotation;
                inputModel.speed = transform.speed;
            }
            return inputModel;
        }
    }
}