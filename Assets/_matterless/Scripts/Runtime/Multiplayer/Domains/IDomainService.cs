using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public interface IDomainService
    {
        bool sessionIdDomain { get; }
        event Action onLightHouseScanFail;
        event Action onLightHouseAssign;
        event Action<DomainStatusEvent> onDomainStateChanged;

        void CreateAsset(AssetId assetId, Pose pose);
        void DeleteDomainAssets();
    }
}