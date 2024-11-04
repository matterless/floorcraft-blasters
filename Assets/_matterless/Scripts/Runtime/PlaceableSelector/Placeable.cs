using UnityEngine;
using UnityEngine.Serialization;
namespace Matterless.Floorcraft
{
	[System.Serializable]
	[CreateAssetMenu(menuName = "Matterless/PlaceableAsset")]
	public class Placeable : Asset
	{
		[SerializeField] private GameObject m_SelectorPrefab;
		[SerializeField] private int m_Style;
		[FormerlySerializedAs("m_LocalisationTag")][SerializeField] private string m_SelectLocalisationTag;
		[SerializeField] private string m_PlaceLocalisationTag;
		[SerializeField] private string m_RemoveLocalisationTag;
		[SerializeField] private string m_RemovesLocalisationTag;

		public GameObject selectorPrefab => m_SelectorPrefab;
		public int style => m_Style;
		public string selectLocalisationTag => m_SelectLocalisationTag;
		public string placeLocalisationTag => m_PlaceLocalisationTag;
		public string removeLocalisationTag => m_RemoveLocalisationTag;
		public string removesLocalisationTag => m_RemovesLocalisationTag;

	}
	
}