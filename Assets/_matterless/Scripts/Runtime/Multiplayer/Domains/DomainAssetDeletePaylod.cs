using UnityEngine;

namespace Matterless.Floorcraft
{
    [System.Serializable]
    public class DomainAssetDeletePayload
    {
        public string app_id;

        /// <summary>
        /// Special app id ("floorcraft" in this case)
        /// </summary>
        /// <param name="appId"></param>
        /// <returns></returns>
        public string CreatePayload(string appId)
        {
            app_id = appId;
            return ToJson();
        }

        public string ToJson() => JsonUtility.ToJson(this);
    }
}