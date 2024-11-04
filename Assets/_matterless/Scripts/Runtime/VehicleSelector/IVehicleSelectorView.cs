using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Matterless.Floorcraft
{
    public interface IVehicleSelectorView
    {
        event Action<PointerEventData> onDrag;
        event Action<PointerEventData> onEndDrag;
        event Action onStoreButtonClicked;
        event Action onNextButtonClicked;
        event Action onPreviousButtonClicked;
        event Action onSelectButtonClicked;
        
        Transform parentTransform { get; }
        GameObject gameObject { get; }
        bool previousButtonEnabled
        {
            get;
            set;
        }
        bool nextButtonEnabled
        {
            get;
            set;
        }
        event Action onSpectatorModeClicked;

        void UpdateView(string name, bool isLocked);
        void Show();
        void Hide();
        VehicleSelectorView Init();
    }
}