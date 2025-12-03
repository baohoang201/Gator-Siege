using System;
using System.Collections.Generic;

namespace UI.Event
{
    public static class UIEvent
    {
        public static event Action<int> OnSelectWeaponEvent;
        public static event Action OnUpdateGemsEvent;
        public static event Action<int, int> OnStartWaveEvent;

        public static event Action<float> OnChangeMusicEvent;
        public static event Action<float> OnChangeSoundFXEvent;

        public static void RaiseSelectWeapon(int weaponIndex)
        {
            OnSelectWeaponEvent?.Invoke(weaponIndex);
        }

        public static void RaiseUpdateGems()
        {
            OnUpdateGemsEvent?.Invoke();
        }

        public static void RaiseStartWave(int currentWave, int totalWaves)
        {
            OnStartWaveEvent?.Invoke(currentWave, totalWaves);
        }

        public static void RaiseChangeMusic(float value)
        {
            OnChangeMusicEvent?.Invoke(value);
        }

        public static void RaiseChangeSoundFX(float value)
        {
            OnChangeSoundFXEvent?.Invoke(value);
        }
    }
}