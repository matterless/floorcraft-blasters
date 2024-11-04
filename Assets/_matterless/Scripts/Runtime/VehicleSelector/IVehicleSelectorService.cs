using System;

namespace Matterless.Floorcraft
{
    public interface IVehicleSelectorService
    {
        void Hide();
        void Show(Action<Vehicle> onVehicleSelected, Action onSpectatorMode);
        void Show();
    }
}