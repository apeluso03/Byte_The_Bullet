using UnityEngine;

namespace Weapons.BeamPhysics
{
    /// <summary>
    /// Interface for beam physics simulation implementations
    /// </summary>
    public interface IBeamPhysics
    {
        /// <summary>
        /// Initialize the beam physics
        /// </summary>
        void Initialize(BeamWeaponConfig config, Transform firePoint);
        
        /// <summary>
        /// Update the beam simulation
        /// </summary>
        /// <param name="startPos">Start position of the beam</param>
        /// <param name="endPos">Target end position of the beam</param>
        /// <param name="fireDirection">Direction the beam is being fired</param>
        void UpdateBeam(Vector3 startPos, Vector3 endPos, Vector3 fireDirection);
        
        /// <summary>
        /// Get the positions of the beam points for rendering
        /// </summary>
        Vector3[] GetBeamPositions();
        
        /// <summary>
        /// Get the end position of the beam (used for effects)
        /// </summary>
        Vector3 GetEndPosition();
        
        /// <summary>
        /// Clean up physics objects when the beam is disabled
        /// </summary>
        void Cleanup();
    }
}