using UnityEngine;

namespace Weapons.BeamPoints
{
    /// <summary>
    /// Represents a point in the beam with follow properties
    /// </summary>
    public class BeamPoint
    {
        public Vector3 Position;
        public Vector3 Target;
        public float FollowSpeed;
        public Vector3 Velocity;
        
        public BeamPoint(Vector3 position, float followSpeed)
        {
            Position = position;
            Target = position;
            FollowSpeed = followSpeed;
            Velocity = Vector3.zero;
        }
        
        public void UpdatePosition(float deltaTime)
        {
            Vector3 direction = Target - Position;
            
            Velocity = Vector3.Lerp(Velocity, direction * FollowSpeed, deltaTime * 10f);
            
            if (Velocity.magnitude > direction.magnitude * 2f)
            {
                Velocity = Velocity.normalized * direction.magnitude * 2f;
            }
            
            Position += Velocity * deltaTime;
        }
    }
}