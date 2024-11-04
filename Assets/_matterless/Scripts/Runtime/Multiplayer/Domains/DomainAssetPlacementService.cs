using Matterless.Inject;

namespace Matterless.Floorcraft
{
    public class DomainAssetPlacementService
    {
        private readonly IRaycastService m_RaycastService;
        private readonly IDomainService m_DomainService;

        public DomainAssetPlacementService(
            IRaycastService raycastService,
            IDomainService domainService)
        {
            m_RaycastService = raycastService;
            m_DomainService = domainService;
        }

        public void CreateAsset()
        {
            m_DomainService.CreateAsset(AssetId.Test, m_RaycastService.hitPose);
        }

        public void ClearAllAssets()
        {
            m_DomainService.DeleteDomainAssets();
        }
    }
}