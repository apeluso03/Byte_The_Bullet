using UnityEngine;

namespace Weapons
{
    /// <summary>
    /// Holds configuration settings for the beam weapon
    /// </summary>
    [System.Serializable]
    public class BeamWeaponConfig
    {
        [Header("Beam Energy")]
        [Tooltip("Maximum energy capacity")]
        public float maxEnergy = 100f;
        
        [Tooltip("Energy regeneration rate per second when not firing")]
        public float energyRegenRate = 15f;
        
        [Tooltip("Energy drain per second while firing beam")]
        public float energyDrainRate = 20f;
        
        [Header("Beam Properties")]
        [Tooltip("Maximum distance the beam can reach")]
        public float beamRange = 20f;
        
        [Tooltip("Width of the beam")]
        [Range(0.05f, 0.5f)]
        public float beamWidth = 0.2f;
        
        [Tooltip("Color of the beam")]
        public Color beamColor = Color.cyan;
        
        [Tooltip("Damage applied per second")]
        public float beamDamagePerSecond = 30f;
        
        [Header("Visual Effects")]
        [Tooltip("Should the weapon play a charge animation before firing?")]
        public bool useChargeEffect = true;
        
        [Tooltip("How long to charge before firing")]
        public float chargeTime = 0.5f;
        
        [Tooltip("Hide the beam color but keep effects visible")]
        public bool hideBeamColor = false;
        
        [Header("Freeze Ray Beam")]
        [Tooltip("Number of segments for the beam (higher = smoother)")]
        [Range(10, 40)]
        public int beamSegments = 24;
        
        [Tooltip("How quickly the beam catches up to your aim direction (lower = more lag)")]
        [Range(1f, 15f)]
        public float beamFollowSpeed = 10f;
        
        [Tooltip("How much the further segments lag behind (higher = more whip effect)")]
        [Range(0.1f, 3f)]
        public float tipLagMultiplier = 1.2f;
        
        [Header("Audio")]
        [Tooltip("Audio clip for beam start")]
        public AudioClip beamStartSound;
        
        [Tooltip("Audio clip for beam loop")]
        public AudioClip beamLoopSound;
        
        [Tooltip("Audio clip for beam end")]
        public AudioClip beamEndSound;
        
        [Header("Rope Physics Beam")]
        [Tooltip("Use verlet integration for a fluid, rope-like beam")]
        public bool useRopePhysics = true;
        
        [Range(0.1f, 1.0f)]
        [Tooltip("How stiff the rope is (lower = more floppy)")]
        public float ropeStiffness = 0.7f;
        
        [Range(5, 50)]
        [Tooltip("Number of simulation iterations (higher = more stable)")]
        public int simulationIterations = 30;
        
        [Range(0.01f, 0.3f)]
        [Tooltip("Distance between points in the beam")]
        public float segmentDistance = 0.1f;
        
        [Header("Smooth Beam Curve")]
        [Tooltip("Use smooth curve interpolation instead of physics")]
        public bool useSmoothCurve = true;
        
        [Range(0.1f, 5f)]
        [Tooltip("How much the beam curves when aiming (higher = more bend)")]
        public float beamCurvature = 1.5f;
        
        [Range(0.1f, 5f)]
        [Tooltip("How quickly the curve follows your aim (lower = more lag)")]
        public float curveFollowSpeed = 2.0f;
        
        [Range(1f, 10f)]
        [Tooltip("How pronounced is the whip effect at the beam's tip")]
        public float whipIntensity = 4.0f;
        
        [Header("Enhanced Smoothing")]
        [Tooltip("Apply extra smoothing passes to eliminate choppiness")]
        public bool useEnhancedSmoothing = true;
        
        [Range(1, 5)]
        [Tooltip("Number of smoothing passes (higher = smoother curves)")]
        public int smoothingPasses = 2;
        
        [Range(0.05f, 0.5f)]
        [Tooltip("Smoothing factor (higher = more smoothing)")]
        public float smoothingStrength = 0.25f;
        
        [Header("Force Visible Curve")]
        [Tooltip("Add a continuous wave motion to the beam")]
        public bool useWaveEffect = true;
        
        [Range(0.2f, 3f)]
        [Tooltip("Amplitude of the wave motion")]
        public float waveAmplitude = 0.8f;
        
        [Range(0.1f, 5f)]
        [Tooltip("Speed of the wave motion")]
        public float waveFrequency = 1.2f;
        
        [Header("Beam Connection Tweaks")]
        [Range(0.05f, 0.5f)]
        [Tooltip("Length of straight section coming out of the gun (as percentage of total beam)")]
        public float straightSectionLength = 0.12f;
        
        [Range(2, 8)]
        [Tooltip("How many points to use for blending between straight and curved sections")]
        public int transitionPointCount = 4;
        
        [Range(0.1f, 5f)]
        [Tooltip("How much the beam is affected by movement (higher = more drag)")]
        public float beamDrag = 0.8f;

        [Range(0.1f, 5f)]
        [Tooltip("Simulated mass at the beam's end (higher = more dramatic whipping)")]
        public float beamTipMass = 2.5f;

        [Range(0.1f, 10f)]
        [Tooltip("How stiff the beam rope is (higher = less floppy)")]
        public float beamStiffness = 1.0f;
    }
}