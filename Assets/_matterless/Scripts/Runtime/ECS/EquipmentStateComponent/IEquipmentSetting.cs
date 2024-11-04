namespace Matterless.Floorcraft
{
	public interface IEquipmentSetting
	{
		public float cooldown { get; }
		public float duration { get; }
		public int quantity { get; }
		public bool infinite { get; }
	}
}
