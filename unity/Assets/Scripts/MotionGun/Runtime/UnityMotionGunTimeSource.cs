using UnityEngine;

namespace MotionGun.Runtime
{
    public sealed class UnityMotionGunTimeSource : IMotionGunTimeSource
    {
        public static readonly UnityMotionGunTimeSource Instance = new UnityMotionGunTimeSource();

        private UnityMotionGunTimeSource()
        {
        }

        public float Time => UnityEngine.Time.time;

        public float DeltaTime => UnityEngine.Time.deltaTime;

        public float RealtimeSinceStartup => UnityEngine.Time.realtimeSinceStartup;
    }
}
