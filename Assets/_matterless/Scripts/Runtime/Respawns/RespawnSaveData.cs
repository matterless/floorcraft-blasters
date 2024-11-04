using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
	[Serializable]
	public class RespawnSaveData
	{
		public long appLastClosedTime;
		public long timer;
		public bool dirty;
		public int respawnQuantity;
		
		public RespawnSaveData(long appLastClosedTime, float timer, int respawnQuantity, bool dirty)
		{
			this.appLastClosedTime = appLastClosedTime;
			this.timer = Convert.ToInt64(timer);
			this.respawnQuantity = respawnQuantity;
			this.dirty = dirty;
		}

		public override string ToString()
		{
			return $"appLastClosedTime: {appLastClosedTime}, timer: {timer} respawnQuantity:{respawnQuantity}, dirty:{dirty}";
		}
	}
}