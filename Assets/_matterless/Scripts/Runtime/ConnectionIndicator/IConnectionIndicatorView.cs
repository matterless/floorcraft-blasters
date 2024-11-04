using UnityEngine;

namespace Matterless.Floorcraft
{
    public interface IConnectionIndicatorView
    {
        void SetVersion(string text);
        void UpdateUI(Color color, string text);
    }
}