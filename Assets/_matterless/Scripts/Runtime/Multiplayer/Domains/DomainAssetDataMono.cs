using UnityEngine;

namespace Matterless.Floorcraft
{
    public class DomainAssetDataMono : MonoBehaviour
    {
        public string assetId { get; private set; }

        public static DomainAssetDataMono AddComponent(GameObject gameObject, string assetId)
        {
            var dataMono = gameObject.AddComponent<DomainAssetDataMono>();
            dataMono.assetId = assetId;
            gameObject.name = assetId;
            return dataMono;
        }
    }
}