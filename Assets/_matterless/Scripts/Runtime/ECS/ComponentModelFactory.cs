using System;
using System.Collections.Generic;

namespace Matterless.Floorcraft
{
    public interface IComponentModelFactory
    {
        Func<uint, uint, bool, IEntityComponentModel> GetFactoryMethod(Type type);
        string GetTypeName(Type type);
    }

    public abstract class BaseComponentModelFactory : IComponentModelFactory
    {
        // bind service to create model method
        private Dictionary<Type, Func<uint, uint, bool, IEntityComponentModel>> m_FactoryMethods = new();
        // bind service to type name
        private Dictionary<Type, string> m_TypeName = new();

        public BaseComponentModelFactory()
        {
            InstallBindings();
        }

        protected abstract void InstallBindings();

        protected void Bind(Type type, string name, Func<uint, uint, bool, IEntityComponentModel> factoryMethod)
        {
            m_FactoryMethods.Add(type, factoryMethod);
            m_TypeName.Add(type, name);
        }

        public Func<uint, uint, bool, IEntityComponentModel> GetFactoryMethod(Type type) => m_FactoryMethods[type];
        public string GetTypeName(Type type) => m_TypeName[type];
    }


    public class ComponentModelFactory : BaseComponentModelFactory
    {
        public ComponentModelFactory()
        {
            
        }

        protected override void InstallBindings()
        {
            // bindings
            Bind(typeof(PropertiesComponentService), "MATTERLESS_FLOORCRAFT_PROPERTY", PropertiesComponentModel.Create);
            Bind(typeof(TransformComponentService), "MATTERLESS_FLOORCRAFT_TRANSFORM", TransformComponentModel.Create);
            Bind(typeof(MessageComponentService), "MATTERLESS_FLOORCRAFT_MESSAGE", MessageComponentModel.Create);
            Bind(typeof(SpeederStateComponentService), "MATTERLESS_FLOORCRAFT_SPEEDER_STATE", SpeederStateComponentModel.Create);
            Bind(typeof(ScoreComponentService), "MATTERLESS_FLOORCRAFT_SCORE", ScoreComponentModel.Create);
            Bind(typeof(NameComponentService), "MATTERLESS_FLOORCRAFT_NAMETAG", NameComponentModel.Create);
            Bind(typeof(CloneComponentService), "MATTERLESS_FLOORCRAFT_CLONE", CloneComponentService.Create);
            Bind(typeof(EquipmentService), "MATTERLESS_FLOORCRAFT_EQUIPMENT", EquipmentStateComponentModel.Create);
            Bind(typeof(PowerUpSpawnPointService), "MATTERLESS_FLOORCRAFT_POWERUPSPAWNPOINT", SpawnPointStateComponentModel.Create);
            Bind(typeof(EnemyStateComponentService), "MATTERLESS_FLOORCRAFT_ENEMY", EnemyStateComponentModel.Create);
            Bind(typeof(MayhemObstacleComponentService), "MATTERLESS_FLOORCRAFT_MAYHEM_OBSTACLE", MayhemObstacleComponentModel.Create);
            Bind(typeof(SpawnLocationsComponentService), "MATTERLESS_FLOORCRAFT_MAYHEM_SPAWN_LOCATIONS", SpawnLocationsComponentModel.Create);
            Bind(typeof(MayhemEnemiesStatusComponentService), "MATTERLESS_FLOORCRAFT_MAYHEM_ENEMY_STATUS", MayhemEnemiesStatusComponentModel.Create);
        }
    }
}