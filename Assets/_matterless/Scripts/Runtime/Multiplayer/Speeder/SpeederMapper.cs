using UnityEngine;

namespace Matterless.Floorcraft
{
    public class SpeederMapper
    {
        public static SpeederViewModel ToViewModel(ISpeederSimulation simulation, EquipmentState equipmentState) =>
            new SpeederViewModel
            (
                simulation.entityId,
                simulation.groundPosition,
                simulation.rotation,
                simulation.state,
                simulation.speed,
                simulation.age,
                simulation.braking,
                simulation.floorNormal,
                simulation.boosting,
                equipmentState,
                simulation.crownKeeper
            );

        public static SpeederGameplayModel ToGameModel(ISpeederSimulation simulation, EquipmentState equipmentState) =>
            new SpeederGameplayModel
            (
                simulation.state,
                simulation.groundPosition,
                simulation.rotation,
                simulation.speed,
                equipmentState
            );
        public static SpeederInputModel ToInputModel(Pose pose, float speed = 0f) =>
            ToInputModel(pose.position, pose.rotation, speed);
        public static SpeederInputModel ToInputModel(
            Vector3 position,
            Quaternion rotation,
            float speed = 0f)
        {
            var inputModel = new SpeederInputModel();
            inputModel.position = position;
            inputModel.rotation = rotation;
            inputModel.speed = speed;
            return inputModel;
        }
        public static SpeederInputModel ToInputModel(TransformModel transform,
            Vector3? target,
            Vector3 floorNormal,
            Vector3 position,
            bool isPlayer,
            bool brakeInput,
            bool boostInput,
            SpeederState speederState,
            EquipmentState equipmentState,
            bool crownKeeper,
            float worldScale)
        {
            var inputModel = new SpeederInputModel();
            if (isPlayer)
            {
                inputModel.target = target;
                inputModel.floorNormal = floorNormal;
                inputModel.position = position;
                inputModel.brake = brakeInput;
                inputModel.input = boostInput;
                inputModel.speederState = speederState;
                inputModel.equipmentState = equipmentState;
                inputModel.crownKeeper = crownKeeper;
                inputModel.worldScale = worldScale;
            }
            else
            {
                inputModel.position = transform.position;
                inputModel.rotation = transform.rotation;
                inputModel.floorNormal = floorNormal;
                inputModel.speed = transform.speed;
                inputModel.speederState = speederState;
                inputModel.equipmentState = equipmentState;
                inputModel.crownKeeper = crownKeeper;
                inputModel.worldScale = worldScale;
            }
            return inputModel;
        }
    }
}