using System;
using UnityEngine;

namespace MotionGun.Gameplay
{
    [Serializable]
    public class WeaponConfig
    {
        [Min(1)] public int SlotId = 1;
        public string DisplayName = "Pistol";
        [Min(1)] public int MagazineSize = 12;
        [Min(0.05f)] public float FireInterval = 0.18f;
        [Min(0.1f)] public float ReloadDuration = 1.1f;
        [Min(0.1f)] public float Damage = 1f;
        [Min(1)] public int ShotsPerTrigger = 1;
        [Min(0.01f)] public float BurstInterval = 0.07f;
        public Transform WeaponVisualRoot;
        [Min(0f)] public float RecoilDistance = 0.08f;
        [Min(1f)] public float RecoilRecoverSpeed = 9f;
    }
}
