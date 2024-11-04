using System;
using System.Collections.Generic;

namespace Matterless.Floorcraft.TestECS
{
    public class TestECSComponentModelFactory : IComponentModelFactory
    {   
        // bind service to create model method
        private Dictionary<Type, Func<uint, uint, bool, IEntityComponentModel>> m_FactoryMethods = new();
        // bind service to type name
        private Dictionary<Type, string> m_TypeName = new();
        public Func<uint, uint, bool, IEntityComponentModel> GetFactoryMethod(Type type) => m_FactoryMethods[type];
        public string GetTypeName(Type type) => m_TypeName[type];

        public TestECSComponentModelFactory()
        {
            m_FactoryMethods.Add(typeof(TestECSTestComponentService), TestECSTestComponentModel.Create);
            m_TypeName.Add(typeof(TestECSTestComponentService), "TEST_ECS_COMPONENT");
        }
    }
}