using System.Runtime.InteropServices;

namespace Matterless.Haptics
{
	public static class HapticManager
	{
#if UNITY_IOS
		[DllImport("__Internal")]
		private static extern void HapticFeedback(int type);
#endif
		public static void StartHapticFeedback(HapticFeedbackTypes type)
		{
			if(type == HapticFeedbackTypes.None)
				return;
			
#if UNITY_IOS && !UNITY_EDITOR
			HapticFeedback((int)type);
#else
			//UnityEngine.Debug.Log($"HapticFeedback: {type}");
#endif
		}
    }

	public enum HapticFeedbackTypes
	{
		None = -1,
		Light = 0,
		Medium = 1,
		Heavy = 2,
		Rigid = 3,
		Soft = 4
	}
}