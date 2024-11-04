namespace Matterless.Floorcraft
{
    [System.Serializable]
    public class DomainAssetResponse
    {
        public string _id;
        public string app_id;
        public string domain_id;
        public DomainAssetData.RawData data;
    }
}