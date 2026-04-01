namespace MotionGun.Runtime
{
    public interface IMotionGunTimeSource
    {
        float Time { get; }
        float DeltaTime { get; }
        float RealtimeSinceStartup { get; }
    }
}
