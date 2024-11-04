using System;

namespace Matterless.Floorcraft
{
    public interface IEntityComponentModel
    {
        bool isMine { get; }
        uint entityId { get; }
        uint typeId { get; }
        byte[] data { get; }

        //abstract void Update(D data);
        abstract void Serialize();
        abstract void Deserialize(byte[] data);

        //#region TO BE REMOVED
        //event Action<uint, byte[]> onAdd;
        //event Action<uint, byte[]> onUpdate;
        //event Action<uint, byte[]> onDelete;
        //#endregion
    }

    public abstract class EntityComponentModel : IEntityComponentModel
    {
        protected const int SIZE_OF_INT = sizeof(int);
        protected const int SIZE_OF_UINT = sizeof(uint);
        protected const int SIZE_OF_FLOAT = sizeof(float);

        public bool isMine { get; private set; }
        public uint entityId { get; private set; }
        public uint typeId { get; private set; }
        public byte[] data => m_Data;

        protected byte[] m_Data;

        public EntityComponentModel(uint typeId, uint entityId, bool isMine)
        {
            this.typeId = typeId;
            this.entityId = entityId;
            this.isMine = isMine;
        }

        public abstract void Serialize();
        public abstract void Deserialize(byte[] data);


        //#region TO BE REMOVED
        //public event Action<uint, byte[]> onAdd;
        //public event Action<uint, byte[]> onUpdate;
        //public event Action<uint, byte[]> onDelete;
        //#endregion

    }
}