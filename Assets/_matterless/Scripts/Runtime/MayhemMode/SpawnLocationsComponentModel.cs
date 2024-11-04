using UnityEngine;

namespace Matterless.Floorcraft
{
    public class SpawnLocationsComponentModel : EntityComponentModel
    {
        public SpawnLocationsModel model { get; set; }
        
        public static SpawnLocationsComponentModel Create(uint typeId, uint entityId, bool isMine)
            => new SpawnLocationsComponentModel(typeId, entityId, isMine);

        public SpawnLocationsComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
            m_Data = new byte[2];
        }
        
        public override void Deserialize(byte[] data)
        {
            Debug.Log($"deserializing {data[0]}");
            byte[] arrayData = data;
            model = new SpawnLocationsModel(arrayData);
        }
        
        public override void Serialize()
        {
            Debug.Log($"serializing data {model.data[0]}");
            m_Data = model.data;
        }
    }
}