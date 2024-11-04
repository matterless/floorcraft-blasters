using Newtonsoft.Json;
using UnityEngine;

namespace Matterless.Floorcraft
{

    [System.Serializable]
    public class DomainAssetData
    {
        private bool m_PoseInitialized;
        private Pose m_Pose;
        private AssetId m_Type;

        //public string id { get; set; }
        public int typeId { get; set; }
        public float[] poseArray { get; set; }
        public string appId { get; set; }
        public bool isValid { get; set; }

        [System.Serializable]
        public class RawData
        {
            public string asset;
        }

        [JsonIgnore]
        public Pose pose
        {
            get
            {
                if (!m_PoseInitialized)
                {

                    m_Pose = new Pose(
                        new Vector3(poseArray[0], poseArray[1], poseArray[2]),
                        new Quaternion(poseArray[3], poseArray[4], poseArray[5], poseArray[6]));
                    m_PoseInitialized = true;
                }

                return m_Pose;
            }

            set
            {
                m_Pose = value;
                poseArray = new float[]
                {
                    value.position.x, value.position.y, value.position.z,
                    value.rotation.x, value.rotation.y, value.rotation.z, value.rotation.w
                };
            }
        }
        
        [JsonIgnore]
        public AssetId type
        {
            get
            {
                m_Type = (AssetId)typeId;
                return m_Type;
            }

            set
            {
                m_Type = value;
                typeId = (int)value;
            }
        }

        public DomainAssetData(AssetId assetId, Pose pose)
        {
            typeId = (int)assetId;
            this.m_Type = assetId;
            this.pose = pose;
            this.appId = DomainService.APP_ID;
            this.isValid = true;
        }

        public string ToJson() => JsonConvert.SerializeObject(this);

        public static DomainAssetData FromJson(string payload)
            => JsonConvert.DeserializeObject<DomainAssetData>(payload);
        
    }
}