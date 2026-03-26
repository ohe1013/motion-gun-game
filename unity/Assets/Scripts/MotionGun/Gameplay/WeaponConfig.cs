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
    }
}
