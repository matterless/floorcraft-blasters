using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class MayhemEnemiesStatusComponentModel : EntityComponentModel
    {
        public MayhemEnemiesStatusModel model { get; set; }
        
        public static MayhemEnemiesStatusComponentModel Create(uint typeId, uint entityId, bool isMine)
            => new MayhemEnemiesStatusComponentModel(typeId, entityId, isMine);

        public MayhemEnemiesStatusComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
        }

        public override void Serialize()
        {
            m_Data = new byte[GetDataSize()];
            int dataOffsetIndex = 0;
            
            // enemy count
            Buffer.BlockCopy(BitConverter.GetBytes(model.enemyCount), 0, m_Data, dataOffsetIndex * SIZE_OF_INT, SIZE_OF_INT);
            
            dataOffsetIndex += SIZE_OF_INT;
            // enemy status models
            for (int i = 0; i < model.enemyModels.Length; i++)
            {
                // coming from the previous array member, which was ended with a float
                if (i > 0)
                {
                    dataOffsetIndex += SIZE_OF_FLOAT;
                }

                // entity id
                Buffer.BlockCopy(BitConverter.GetBytes(model.enemyModels[i].entityId), 0, m_Data, dataOffsetIndex, SIZE_OF_UINT);
                // vector3
                // x
                dataOffsetIndex += SIZE_OF_UINT;
                Buffer.BlockCopy(BitConverter.GetBytes(model.enemyModels[i].position.x), 0, m_Data, dataOffsetIndex, SIZE_OF_FLOAT);
                // y
                dataOffsetIndex += SIZE_OF_FLOAT;
                Buffer.BlockCopy(BitConverter.GetBytes(model.enemyModels[i].position.y), 0, m_Data, dataOffsetIndex, SIZE_OF_FLOAT);
                // z
                dataOffsetIndex += SIZE_OF_FLOAT;
                Buffer.BlockCopy(BitConverter.GetBytes(model.enemyModels[i].position.z), 0, m_Data, dataOffsetIndex, SIZE_OF_FLOAT);
                // speed
                dataOffsetIndex += SIZE_OF_FLOAT;
                Buffer.BlockCopy(BitConverter.GetBytes(model.enemyModels[i].speed), 0, m_Data, dataOffsetIndex, SIZE_OF_FLOAT);
            }
        }
        
        public override void Deserialize(byte[] data)
        {
            int dataOffsetIndex = 0;
            int enemyCount = BitConverter.ToInt32(data, dataOffsetIndex);

            MayhemEnemyStatusModel[] enemyStatusModels = new MayhemEnemyStatusModel[enemyCount];
            
            dataOffsetIndex += SIZE_OF_INT;
            for (int i = 0; i < enemyCount; i++)
            {
                // coming from the previous array member, which was ended with a float
                if (i > 0)
                {
                    dataOffsetIndex += SIZE_OF_FLOAT;
                }
                
                uint entityId = BitConverter.ToUInt32(data, dataOffsetIndex);

                Vector3 position = new Vector3();
                dataOffsetIndex += SIZE_OF_UINT;
                position.x = BitConverter.ToSingle(data, dataOffsetIndex);

                dataOffsetIndex += SIZE_OF_FLOAT;
                position.y = BitConverter.ToSingle(data, dataOffsetIndex);
                
                dataOffsetIndex += SIZE_OF_FLOAT;
                position.z = BitConverter.ToSingle(data, dataOffsetIndex);
                
                dataOffsetIndex += SIZE_OF_FLOAT;
                float speed = BitConverter.ToSingle(data, dataOffsetIndex);

                enemyStatusModels[i] = new MayhemEnemyStatusModel(entityId, position, speed);
            }

            model = new MayhemEnemiesStatusModel(enemyCount, enemyStatusModels);
        }

        private int GetDataSize()
        {
            int enemyCountDataSize = SIZE_OF_INT;
            
            // Respectively: Entity Id, Vector3 position, Speed
            int enemyStatusModelDataSize = SIZE_OF_UINT + (3 * SIZE_OF_FLOAT) + SIZE_OF_FLOAT;

            return enemyCountDataSize + (enemyStatusModelDataSize * model.enemyModels.Length);
        }
    }
}