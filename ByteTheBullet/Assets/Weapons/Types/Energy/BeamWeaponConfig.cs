using UnityEngine;

namespace Weapons
{
    /// <summary>
    /// Holds configuration settings for the beam weapon
    /// </summary>
    [System.Serializable]
    public class BeamWeaponConfig
    {
        // Define a single fire mode enum with fewer options
        public enum BeamFireMode
        {
            Continuous,  // Standard continuous beam
            ChargeBurst  // Auto-fire burst when charged
        }
        
        [Header("Beam Firing")]
        [Tooltip("The firing pattern of the beam weapon")]
        public BeamFireMode fireMode = BeamFireMode.Continuous;
        
        // For continuous mode
        [Tooltip("Time to charge before the continuous beam is ready (seconds)")]
        [Range(0f, 2f)]
        public float continuousChargeTime = 0.2f;
        
        // For charge burst mode
        [Tooltip("Time to reach maximum charge (seconds)")]
        [Range(0.5f, 3f)]
        public float maxChargeTime = 1.5f;
        
        [Tooltip("Damage multiplier at full charge")]
        [Range(1f, 5f)]
        public float maxChargeDamageMultiplier = 2.5f;
        
        [Tooltip("How long the burst fires (seconds)")]
        [Range(0.1f, 2.0f)]
        public float burstDuration = 0.5f;
        
        [Header("Beam Energy")]
        [Tooltip("Maximum energy capacity")]
        public float maxEnergy = 100f;
        
        [Tooltip("Energy drain per second while firing beam")]
        public float energyDrainRate = 20f;
        
        [Header("Energy System")]
        [Tooltip("How the beam's energy is replenished")]
        public EnergySystemType energySystemType = EnergySystemType.AutoRecharge;
        
        // Define the energy system types
        public enum EnergySystemType
        {
            AutoRecharge,  // Energy regenerates over time automatically
            BatteryReload  // Requires manual reload with batteries
        }
        
        [Header("Auto-Recharge Settings")]
        [Tooltip("Energy regeneration rate per second when not firing")]
        public float energyRegenRate = 15f;
        
        [Header("Battery Reload Settings")]
        [Tooltip("Maximum number of batteries that can be carried")]
        public int maxBatteryCount = 5;
        
        [Tooltip("Current number of batteries")]
        [SerializeField] private int currentBatteryCount = 3;
        
        [Tooltip("Energy contained in each battery")]
        public float energyPerBattery = 50f;
        
        [Tooltip("Time required to reload a battery")]
        public float batteryReloadTime = 2.0f;
        
        [Tooltip("Automatically reload when energy is depleted")]
        public bool autoReloadWhenDepleted = true;
        
        // Property to access current battery count
        public int CurrentBatteryCount
        {
            get => currentBatteryCount;
            set => currentBatteryCount = Mathf.Clamp(value, 0, maxBatteryCount);
        }
        
        [Header("Beam Properties")]
        [Tooltip("Maximum distance the beam can reach")]
        public float beamRange = 20f;
        
        [Tooltip("Width of the beam")]
        [Range(0.05f, 1.0f)]
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

        [Header("Visual FX Prefabs")]
        [Tooltip("Prefab for the charge effect displayed when charging the beam")]
        public GameObject chargeFXPrefab;

        [Tooltip("Prefab for the flare effect displayed at beam start and end points")]
        public GameObject flareFXPrefab;

        [Tooltip("Prefab for the impact effect displayed where the beam hits")]
        public GameObject impactFXPrefab;

        [Tooltip("Prefab for the beam middle sections")]
        public GameObject beamMiddleAnimPrefab;

        [Tooltip("Distance between repeated beam middle sprites")]
        [Range(0.1f, 5.0f)]
        public float beamSectionDistance = 1.0f;

        [Tooltip("How much sections should overlap (0-1). Higher values close gaps between sections.")]
        [Range(0f, 0.9f)]
        public float sectionOverlap = 0.3f;

        [Header("Water Hose Effect")]
        [Tooltip("How much the beam oscillates when moving (higher = more wobble)")]
        [Range(0.1f, 2.0f)]
        public float hoseWobbleIntensity = 0.5f;

        [Tooltip("How quickly the beam oscillates (higher = faster wobble)")]
        [Range(0.5f, 5.0f)]
        public float hoseWobbleSpeed = 2.0f;

        [Tooltip("How much the beam lags behind aim changes (higher = more lag)")]
        [Range(0.1f, 3.0f)]
        public float hoseLagIntensity = 1.0f;

        [Tooltip("How much gravity affects the beam (higher = more sag)")]
        [Range(0f, 1.0f)]
        public float hoseGravityEffect = 0.2f;

        [Header("Beam Stability")]
        [Tooltip("Portion of beam near the weapon that remains stiff (0-0.5)")]
        [Range(0f, 0.5f)]
        public float stiffPortionLength = 0.15f;

        [Header("Beam Position Adjustment")]
        [Tooltip("Vertical offset to raise/lower the beam (positive = higher)")]
        [Range(-1f, 1f)]
        public float beamHeightOffset = 0f;

        [Tooltip("Horizontal offset to shift the beam's starting point (positive = forward)")]
        [Range(-1f, 1f)]
        public float beamForwardOffset = 0f;

        [Tooltip("Show GUI sliders to adjust beam position in play mode")]
        public bool showPositionAdjustmentGUI = true;

        [Tooltip("Show GUI slider to adjust beam height in play mode")]
        public bool showHeightAdjustmentGUI = true;
    }
}