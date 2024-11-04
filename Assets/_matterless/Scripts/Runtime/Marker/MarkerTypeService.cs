namespace Matterless.Floorcraft
{
	public class MarkerTypeService
	{
		public MarkerService.MarkerType GetType() => m_Type;
		public void SetType(MarkerService.MarkerType markerType) => m_Type = markerType;
		private MarkerService.MarkerType m_Type;
	}
}