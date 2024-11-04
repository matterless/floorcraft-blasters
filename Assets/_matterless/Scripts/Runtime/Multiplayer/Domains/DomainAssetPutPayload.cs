using UnityEngine;

namespace Matterless.Floorcraft
{
    [System.Serializable]
    public class DomainAssetPutPayload
    {
        public string updated_at;
        public string domain_id;
        public string data;

        /// <summary>
        /// UpdatedAt is required. 
        /// </summary>
        /// <param name="updatedAt">Unix timestamp in ms</param>
        /// <param name="domainId"></param>
        /// <param name="data">Any data in JSON format</param>
        /// <returns></returns>
        public string CreatePayload(string updatedAt, string domainId, string data)
        {
            updated_at = updatedAt;
            domain_id = domainId;
            this.data = data;
            return ToJson();
        }

        public string ToJson() => JsonUtility.ToJson(this);
    }
}