using UnityEngine;

namespace Weapons.BeamPoints
{
    /// <summary>
    /// Represents a point in the rope physics simulation
    /// </summary>
    public class RopePoint
    {
        public Vector3 Position;
        public Vector3 OldPosition;
        public bool IsLocked;

        public RopePoint(Vector3 pos, bool locked = false)
        {
            Position = pos;
            OldPosition = pos;
            IsLocked = locked;
        }
    }
}