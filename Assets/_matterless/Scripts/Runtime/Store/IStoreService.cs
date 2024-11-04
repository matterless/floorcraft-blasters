using System;

namespace Matterless.Floorcraft
{
    public interface IStoreService
    {
        bool isPremiumUnlocked { get; }
        bool premiumEnabled { get; }
        public event Action onPremiumUnlocked;

        void Show();
        void Hide();
    }
}