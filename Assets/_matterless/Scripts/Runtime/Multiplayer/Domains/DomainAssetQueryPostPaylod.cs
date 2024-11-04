using UnityEngine;

namespace Matterless.Floorcraft
{
    [System.Serializable]
    public class DomainAssetQueryPostPayload
    {
        public string app_id;
        public string domain_id;

        public string CreatePayload(string appId, string domainId)
        {
            app_id = appId;
            domain_id = domainId;
            return ToJson();
        }

        public string ToJson() => JsonUtility.ToJson(this);
    }
}