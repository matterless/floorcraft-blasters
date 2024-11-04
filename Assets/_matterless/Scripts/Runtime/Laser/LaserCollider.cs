using UnityEngine;
namespace Matterless.Floorcraft
{
	public class LaserCollider : GameObjectView
	{
		public CapsuleCollider capsuleCollider => m_CapsuleCollider;
		
		[SerializeField] private CapsuleCollider m_CapsuleCollider;
		public void Init(uint id)
		{
			entityId = id;
		}
	}
}