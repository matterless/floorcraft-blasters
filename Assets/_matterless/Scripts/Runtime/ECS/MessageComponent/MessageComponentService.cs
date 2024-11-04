namespace Matterless.Floorcraft
{
    /// <summary>
    /// 
    /// an entity has ECS component
    /// update the component 3 times in a frame (in an update cycle)
    /// the ECS is going to update only the last one
    /// 
    /// We need to replace this with custom messages
    /// </summary>

    public class MessageComponentService : GenericComponentService<MessageComponentModel, MessageModel>
    {
        public MessageComponentService(IECSController ecsController, IComponentModelFactory componentModelFactory) : base(ecsController, componentModelFactory)
        {
        }

        public void SendMessage(uint entityId, MessageModel.Message message, uint otherEntityId)
        {
            // send message
            UpdateComponent(entityId, new MessageModel(message, otherEntityId));
        }

        protected override void UpdateComponentMethod(MessageComponentModel model, MessageModel data)
        {
            model.model = data;
        }
    }
}