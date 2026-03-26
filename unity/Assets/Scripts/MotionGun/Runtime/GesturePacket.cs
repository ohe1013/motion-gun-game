using System;

namespace MotionGun.Runtime
{
    [Serializable]
    public class GesturePacket
    {
        public float aim_x = 0.5f;
        public float aim_y = 0.5f;
        public bool fire;
        public bool reload;
        public int weapon_slot = -1;
        public float tracking_confidence;
        public bool primary_hand_detected;
        public bool secondary_hand_detected;
        public double timestamp_seconds;
    }
}
