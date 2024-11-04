namespace Matterless.Floorcraft
{
    public interface IHapticService
    {
        void PlayHeavyHapticsTwice();
        void PlayHeavyHaptics();
        void PlayLightHaptics();
        void PlayMediumHaptics();
        void PlayRigidHaptics();
        void PlaySoftHaptics();
    }
}