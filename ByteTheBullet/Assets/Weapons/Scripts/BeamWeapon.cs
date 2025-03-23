using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Weapons;

namespace Weapons
{
    public class BeamWeapon : BaseWeapon
    {
        [Header("Beam Energy")]
        [Tooltip("Maximum energy capacity")]
        public float maxEnergy = 100f;
        
        [Tooltip("Current energy level")]
        [SerializeField] private float currentEnergy;
        
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
        [Tooltip("Reference to the BeamWeaponFX component")]
        public BeamWeaponFX beamFX;
        
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
        
        // Add these for forced curvature
        [Header("Force Visible Curve")]
        [Tooltip("Add a continuous wave motion to the beam")]
        public bool useWaveEffect = true;
        
        [Range(0.2f, 3f)]
        [Tooltip("Amplitude of the wave motion")]
        public float waveAmplitude = 0.8f;
        
        [Range(0.1f, 5f)]
        [Tooltip("Speed of the wave motion")]
        public float waveFrequency = 1.2f;
        
        // Add this new property to control the straight section
        [Header("Beam Connection Tweaks")]
        [Range(0.05f, 0.5f)]
        [Tooltip("Length of straight section coming out of the gun (as percentage of total beam)")]
        public float straightSectionLength = 0.12f; // Reduced from 0.2 to 0.12 (12% of beam)
        
        [Range(2, 8)]
        [Tooltip("How many points to use for blending between straight and curved sections")]
        public int transitionPointCount = 4; // Number of points in transition zone
        
        // Private state
        private bool isFiringBeam = false;
        private bool isCharging = false;
        private LineRenderer beamLine;
        private Coroutine chargeCoroutine;
        
        // Beam curve tracking
        private List<BeamPoint> beamPoints = new List<BeamPoint>();
        private Vector3 targetEndPoint;
        private Vector3 currentEndPoint;
        private Quaternion currentEndRotation;
        
        // Physics simulation objects
        private Transform[] beamJoints;
        private GameObject beamRoot;
        private Rigidbody2D[] beamRigidbodies;
        private SpringJoint2D[] beamSprings;
        private TrailRenderer trailRenderer;
        
        // Verlet integration point structure
        private class RopePoint
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

        private List<RopePoint> ropePoints = new List<RopePoint>();
        private float segmentLength;
        private Vector3 lastAimPosition;
        
        // Public properties
        public float CurrentEnergy
        {
            get => currentEnergy;
            set => currentEnergy = Mathf.Clamp(value, 0, maxEnergy);
        }
        
        public bool IsFiring => isFiringBeam;
        
        // Class to track beam points with their own follow properties
        private class BeamPoint
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
        
        // Add this as a class variable to track aim direction changes
        private Vector3 lastAimDirection = Vector3.right;
        
        // Add these as private fields
        private Vector3 lastFireDirection; // Last frame's firing direction
        private Vector3 fireVelocity; // How quickly the aim is changing
        private List<Vector3> curvePoints = new List<Vector3>(); // Points along the curve
        private List<Vector3> targetCurvePoints = new List<Vector3>(); // Where points should move to
        private Transform beamContainer; // Container for beam sections
        
        // Add these missing physics properties
        [Range(0.1f, 5f)]
        [Tooltip("How much the beam is affected by movement (higher = more drag)")]
        public float beamDrag = 0.8f;

        [Range(0.1f, 5f)]
        [Tooltip("Simulated mass at the beam's end (higher = more dramatic whipping)")]
        public float beamTipMass = 2.5f;

        [Range(0.1f, 10f)]
        [Tooltip("How stiff the beam rope is (higher = less floppy)")]
        public float beamStiffness = 1.0f;
        
        protected override void Awake()
        {
            base.Awake();
            CurrentEnergy = maxEnergy;
            
            // If beamFX is not assigned, try to find or add it
            if (beamFX == null)
            {
                beamFX = GetComponent<BeamWeaponFX>();
                if (beamFX == null)
                {
                    beamFX = gameObject.AddComponent<BeamWeaponFX>();
                }
            }
            
            InitializeBeam();
        }
        
        private void InitializeBeam()
        {
            // Set up the beam renderer
            if (beamLine == null)
            {
                beamLine = gameObject.AddComponent<LineRenderer>();
                beamLine.startWidth = beamWidth;
                beamLine.endWidth = beamWidth * 0.7f;
                beamLine.positionCount = beamSegments;
                beamLine.useWorldSpace = true;
                beamLine.alignment = LineAlignment.View;
                
                // Set up corners for rounded appearance
                beamLine.numCapVertices = 6;
                beamLine.numCornerVertices = 6;
                
                // Set up material
                if (beamFX != null)
                {
                    beamFX.SetupBeamMaterial(beamLine, beamColor);
                }
                else
                {
                    Material beamMaterial = new Material(Shader.Find("Sprites/Default"));
                    beamMaterial.SetColor("_Color", beamColor);
                    beamLine.material = beamMaterial;
                    
                    beamLine.startColor = beamColor;
                    beamLine.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0.7f);
                }
                
                beamLine.enabled = false;
            }
            
            // Initialize the beam points
            beamPoints.Clear();
            
            // Default positions - will be updated when fired
            for (int i = 0; i < beamSegments; i++)
            {
                float t = (float)i / (beamSegments - 1);
                
                // Points further along the beam react more slowly (higher whip effect)
                float followMultiplier = Mathf.Pow(1f - t, tipLagMultiplier);
                float pointFollowSpeed = beamFollowSpeed * followMultiplier;
                
                beamPoints.Add(new BeamPoint(Vector3.zero, pointFollowSpeed));
            }
            
            UpdateBeamVisibility();
        }
        
        public void UpdateBeamVisibility()
        {
            if (beamLine != null)
            {
                if (hideBeamColor)
                {
                    beamLine.startColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0f);
                    beamLine.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0f);
                }
                else
                {
                    beamLine.startColor = beamColor;
                    beamLine.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0.7f);
                }
            }
        }
        
        public void ToggleBeamVisibility()
        {
            hideBeamColor = !hideBeamColor;
            UpdateBeamVisibility();
        }
        
        protected override void Update()
        {
            HandleInput();
            RegenerateEnergy();
            
            if (isFiringBeam)
            {
                UpdateBeam();
            }
        }
        
        private void HandleInput()
        {
            // Get the weapon aiming component to check if we're equipped
            WeaponAiming aiming = GetComponent<WeaponAiming>();
            if (aiming != null && !aiming.isEquipped)
            {
                if (isFiringBeam)
                {
                    StopBeam();
                }
                return;
            }
            
            // Check for energy recharge (reload equivalent)
            if (Input.GetKeyDown(KeyCode.R) && CurrentEnergy < maxEnergy)
            {
                StartCoroutine(RechargeBeam());
                return;
            }
            
            // Handle firing input
            if (Input.GetMouseButtonDown(0) && !isFiringBeam && CanFireBeam())
            {
                StartBeam();
            }
            else if (Input.GetMouseButtonUp(0) && isFiringBeam)
            {
                StopBeam();
            }
        }
        
        private bool CanFireBeam()
        {
            return CurrentEnergy > 0 && !isReloading;
        }
        
        private void RegenerateEnergy()
        {
            // Only regenerate when not firing and not at max energy
            if (!isFiringBeam && !isReloading && CurrentEnergy < maxEnergy)
            {
                CurrentEnergy += energyRegenRate * Time.deltaTime;
                
                // Trigger ammo changed event (for UI updates)
                if (onAmmoChanged != null)
                {
                    int currentEnergyInt = Mathf.FloorToInt(CurrentEnergy);
                    onAmmoChanged.Invoke(currentEnergyInt, Mathf.FloorToInt(maxEnergy));
                }
            }
        }
        
        private IEnumerator RechargeBeam()
        {
            isReloading = true;
            onReloadStart?.Invoke();
            
            // Play recharge sound
            if (audioSource != null && reloadSound != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }
            
            // Calculate recharge time based on how depleted we are
            float rechargeTime = reloadTime * (1 - CurrentEnergy / maxEnergy);
            float startEnergy = CurrentEnergy;
            float elapsed = 0f;
            
            // Gradually recharge over time
            while (elapsed < rechargeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / rechargeTime;
                CurrentEnergy = Mathf.Lerp(startEnergy, maxEnergy, t);
                
                // Update UI
                if (onAmmoChanged != null)
                {
                    int currentEnergyInt = Mathf.FloorToInt(CurrentEnergy);
                    onAmmoChanged.Invoke(currentEnergyInt, Mathf.FloorToInt(maxEnergy));
                }
                
                yield return null;
            }
            
            // Ensure we're fully charged
            CurrentEnergy = maxEnergy;
            isReloading = false;
            onReloadComplete?.Invoke();
        }
        
        public void StartBeam()
        {
            if (!CanFireBeam())
                return;
            
            // If using charge effect, start the charging sequence
            if (useChargeEffect && chargeTime > 0)
            {
                if (!isCharging)
                {
                    if (chargeCoroutine != null)
                        StopCoroutine(chargeCoroutine);
                        
                    chargeCoroutine = StartCoroutine(ChargeAndFireBeam());
                }
            }
            else
            {
                // Fire immediately if not using charge effect
                ActivateBeam();
            }
        }
        
        private IEnumerator ChargeAndFireBeam()
        {
            isCharging = true;
            
            // Play charge sound
            if (audioSource != null && beamStartSound != null)
            {
                audioSource.PlayOneShot(beamStartSound, 0.5f);
            }
            
            // Play charge effect
            if (beamFX != null)
            {
                beamFX.PlayChargeEffect(firePoint);
            }
            
            // Wait for charge duration
            yield return new WaitForSeconds(chargeTime);
            
            // Fire the beam
            ActivateBeam();
            isCharging = false;
        }
        
        private void ActivateBeam()
        {
            isFiringBeam = true;
            
            // Stop charge effect if needed
            if (beamFX != null && isCharging)
            {
                beamFX.StopChargeEffect();
            }
            
            Vector3 startPos = firePoint.position;
            Vector3 aimDirection = firePoint.right;
            Vector3 endPos = startPos + aimDirection * beamRange;
            
            // Initialize fire velocity
            fireVelocity = Vector3.zero;
            lastAimPosition = endPos;
            
            // Calculate how many segments we need
            int pointCount = Mathf.CeilToInt(beamRange / segmentDistance) + 1;
            pointCount = Mathf.Max(pointCount, 30); // Increased for smoother curve
            
            segmentLength = beamRange / (pointCount - 1);
            
            // Initialize rope points with an initial curve
            ropePoints.Clear();
            
            // Create the straight section points
            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / (pointCount - 1);
                
                // Start with a slightly curved shape
                Vector3 basePos = startPos + aimDirection * beamRange * t;
                
                // Add a slight initial curve for more natural appearance
                if (i > 1)
                {
                    Vector3 perpDir = new Vector3(-aimDirection.y, aimDirection.x, 0);
                    float sinCurve = Mathf.Sin(t * Mathf.PI);
                    float curveAmt = sinCurve * 0.05f * beamRange; // Very subtle initial curve
                    basePos += perpDir * curveAmt;
                }
                
                // Only the first point is locked
                bool isLocked = (i == 0);
                RopePoint point = new RopePoint(basePos, isLocked);
                ropePoints.Add(point);
                
                // Give slight variation to old positions to create initial movement
                if (i > 1)
                {
                    float offset = 0.01f * (1.0f - t); // Smaller offset further along beam
                    point.OldPosition = basePos + Random.insideUnitSphere * offset;
                }
                else
                {
                    point.OldPosition = basePos;
                }
            }
            
            // Play audio effects
            if (audioSource != null && !isCharging && beamStartSound != null)
            {
                audioSource.PlayOneShot(beamStartSound);
            }
            
            if (audioSource != null && beamLoopSound != null)
            {
                audioSource.clip = beamLoopSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            
            // Invoke events
            onFire?.Invoke();
            
            // Store initial aim
            lastAimDirection = firePoint.right;
        }
        
        private void UpdateBeam()
        {
            // Energy consumption code remains the same
            CurrentEnergy -= energyDrainRate * Time.deltaTime;
            
            // Update UI
            if (onAmmoChanged != null)
            {
                int currentEnergyInt = Mathf.FloorToInt(CurrentEnergy);
                onAmmoChanged.Invoke(currentEnergyInt, Mathf.FloorToInt(maxEnergy));
            }
            
            // If energy is depleted, stop the beam
            if (CurrentEnergy <= 0)
            {
                StopBeam();
                return;
            }
            
            // Calculate beam start and target end positions
            Vector3 startPos = firePoint.position;
            Vector3 aimDirection = firePoint.right;
            
            // Raycast to find what the beam hits
            RaycastHit2D hit = Physics2D.Raycast(startPos, aimDirection, beamRange);
            
            Vector3 endPos;
            bool hasHit = false;
            Vector3 hitNormal = Vector3.zero;
            
            if (hit.collider != null)
            {
                endPos = hit.point;
                hasHit = true;
                hitNormal = hit.normal;
                
                // Apply damage to hit object (unchanged)
                float damageThisFrame = beamDamagePerSecond * Time.deltaTime;
                // Your damage code would go here
            }
            else
            {
                endPos = startPos + aimDirection * beamRange;
            }
            
            // Update the rope physics simulation
            if (useRopePhysics && ropePoints.Count > 0)
            {
                UpdateRopePhysics(startPos, endPos);
            }
            
            // Update end rotation for effects
            Quaternion targetRotation = hasHit ? 
                Quaternion.FromToRotation(Vector3.right, hitNormal) : firePoint.rotation;
            
            // The rest of your effect update code as needed
            if (beamFX != null)
            {
                // FIXED: Check if ropePoints has any elements before accessing
                Vector3 effectEndPos;
                if (useRopePhysics && ropePoints.Count > 0)
                {
                    effectEndPos = ropePoints[ropePoints.Count - 1].Position;
                }
                else if (beamPoints.Count > 0)
                {
                    effectEndPos = beamPoints[beamPoints.Count - 1].Position;
                }
                else
                {
                    effectEndPos = endPos; // Fallback to calculated endpoint
                }
                
                beamFX.PlayFlareEffects(startPos, effectEndPos, firePoint.rotation, targetRotation);
                
                if (hasHit)
                {
                    beamFX.PlayImpactEffect(effectEndPos, hitNormal);
                }
                else
                {
                    beamFX.StopImpactEffect();
                }
                
                // Convert rope points to array for FX - FIXED
                if (useRopePhysics && ropePoints.Count > 0)
                {
                    Vector3[] pointsArray = new Vector3[ropePoints.Count];
                    for (int i = 0; i < ropePoints.Count; i++)
                    {
                        pointsArray[i] = ropePoints[i].Position;
                    }
                    beamFX.UpdateCurvedBeamAnimation(pointsArray);
                }
            }
        }
        
        private void UpdateRopePhysics(Vector3 startPos, Vector3 endPos)
        {
            if (ropePoints.Count < 2) return;
            
            float dt = Time.deltaTime;
            
            // Calculate the aim direction and ideal target positions
            Vector3 aimDirection = (endPos - startPos).normalized;
            
            // Calculate aim velocity with smoother dampening
            Vector3 targetVelocity = (endPos - lastAimPosition) / dt;
            Vector3 aimVelocity = Vector3.Lerp(fireVelocity, targetVelocity, dt * 4f);
            fireVelocity = aimVelocity;
            lastAimPosition = endPos;
            
            // Limit extreme aim velocities
            float maxAimSpeed = 8f;
            if (aimVelocity.magnitude > maxAimSpeed)
            {
                aimVelocity = aimVelocity.normalized * maxAimSpeed;
            }
            
            // Lock the first point to the gun
            ropePoints[0].Position = startPos;
            ropePoints[0].OldPosition = startPos;
            
            // Lock the last point to the target position - CRITICAL FIX
            ropePoints[ropePoints.Count - 1].Position = endPos;
            ropePoints[ropePoints.Count - 1].OldPosition = endPos;
            
            // Special handling for second point - creates a small straight section
            if (ropePoints.Count > 1)
            {
                Vector3 secondPointPos = startPos + aimDirection * segmentLength * 1.2f;
                ropePoints[1].Position = secondPointPos;
                ropePoints[1].OldPosition = Vector3.Lerp(ropePoints[1].OldPosition, secondPointPos, dt * 15f);
            }
            
            // Create an ideal curve for the beam to follow
            Vector3[] idealPoints = new Vector3[ropePoints.Count];
            idealPoints[0] = startPos;
            idealPoints[ropePoints.Count - 1] = endPos; // Make sure end point is set
            
            // Calculate the length of the beam
            float totalLength = segmentLength * (ropePoints.Count - 1);
            
            // Calculate perpendicular vector for whip motion
            Vector3 perpendicular = new Vector3(-aimDirection.y, aimDirection.x, 0).normalized;
            
            // Direction to curve (based on aim movement)
            float curveSide = Mathf.Sign(Vector3.Dot(aimVelocity, perpendicular));
            perpendicular *= curveSide;
            
            // Create the ideal curve with strong whip towards the end (skip first and last points)
            for (int i = 2; i < ropePoints.Count - 1; i++)
            {
                float t = (float)i / (ropePoints.Count - 1);
                
                // Base straight position
                Vector3 straightPos = startPos + aimDirection * (t * totalLength);
                
                // Apply curve based on distance from gun (stronger at tip)
                float curveFactor = Mathf.Pow(t, 2) * Mathf.Min(aimVelocity.magnitude * 0.025f, 0.5f);
                
                // Calculate the arc
                Vector3 offset = perpendicular * curveFactor * totalLength;
                
                // Modulate with sine wave for smoother arc
                offset *= Mathf.Sin(t * Mathf.PI);
                
                idealPoints[i] = straightPos + offset;
            }
            
            // Apply verlet integration for points between first and last
            for (int i = 2; i < ropePoints.Count - 1; i++)
            {
                // Get the previous position
                Vector3 temp = ropePoints[i].Position;
                
                // Apply verlet physics
                Vector3 velocity = ropePoints[i].Position - ropePoints[i].OldPosition;
                
                // INCREASED LAG: Preserve more of the old velocity
                velocity *= 0.98f;
                
                // INCREASED LAG: Progressively slower follow from gun to tip
                Vector3 targetPos = idealPoints[i];
                float t = (float)i / (ropePoints.Count - 1);
                
                // Reduced follow strength toward ideal points
                float followStrength = dt * (7.0f - t * 3.0f) * (1.0f + i * 0.1f);
                
                // Apply movement - combined physics and target following
                ropePoints[i].Position += velocity;
                ropePoints[i].Position = Vector3.Lerp(ropePoints[i].Position, targetPos, followStrength);
                
                // Store old position
                ropePoints[i].OldPosition = temp;
            }
            
            // Apply distance constraints except for fixed end points
            for (int iteration = 0; iteration < simulationIterations; iteration++)
            {
                // Process all segments except the last one
                for (int i = 0; i < ropePoints.Count - 2; i++)
                {
                    RopePoint p1 = ropePoints[i];
                    RopePoint p2 = ropePoints[i + 1];
                    
                    Vector3 delta = p2.Position - p1.Position;
                    float distance = delta.magnitude;
                    float error = distance - segmentLength;
                    
                    Vector3 direction = delta.normalized;
                    
                    if (i == 0)
                    {
                        // First point is fixed, only move second point
                        p2.Position -= direction * error;
                    }
                    else
                    {
                        // Normal constraint handling
                        p1.Position += direction * error * 0.5f;
                        p2.Position -= direction * error * 0.5f;
                    }
                }
                
                // Handle last segment separately to preserve endpoint position
                int lastIndex = ropePoints.Count - 2;
                RopePoint lastSegStart = ropePoints[lastIndex];
                RopePoint lastSegEnd = ropePoints[lastIndex + 1]; // The end point
                
                Vector3 lastDelta = lastSegEnd.Position - lastSegStart.Position;
                float lastDistance = lastDelta.magnitude;
                float lastError = lastDistance - segmentLength;
                
                if (lastError != 0)
                {
                    Vector3 direction = lastDelta.normalized;
                    // Only move the second-to-last point, keeping the end fixed
                    lastSegStart.Position += direction * lastError;
                }
            }
            
            // Apply smoother passes but preserve endpoints
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 2; i < ropePoints.Count - 2; i++)
                {
                    Vector3 prev = ropePoints[i - 1].Position;
                    Vector3 curr = ropePoints[i].Position;
                    Vector3 next = ropePoints[i + 1].Position;
                    
                    Vector3 smoothPos = (prev + next) * 0.5f;
                    
                    // Progressive smoothing factor (less aggressive, more laggy)
                    float smoothFactor = ropeStiffness * 0.7f * (1.0f - (i / (float)ropePoints.Count) * 0.5f);
                    ropePoints[i].Position = Vector3.Lerp(curr, smoothPos, smoothFactor);
                }
                
                // Special handling for second-to-last point - gentler smoothing to preserve endpoint
                int preLastIndex = ropePoints.Count - 2;
                if (preLastIndex > 2)
                {
                    Vector3 prev = ropePoints[preLastIndex - 1].Position;
                    Vector3 curr = ropePoints[preLastIndex].Position;
                    Vector3 next = endPos; // Use exact endpoint
                    
                    float smoothFactor = ropeStiffness * 0.3f;
                    Vector3 smoothPos = (prev + next) * 0.5f;
                    ropePoints[preLastIndex].Position = Vector3.Lerp(curr, smoothPos, smoothFactor);
                }
            }
            
            // Update the line renderer
            if (beamLine != null)
            {
                beamLine.enabled = true;
                beamLine.positionCount = ropePoints.Count;
                
                for (int i = 0; i < ropePoints.Count; i++)
                {
                    beamLine.SetPosition(i, ropePoints[i].Position);
                }
            }
        }
        
        public void StopBeam()
        {
            if (isCharging && chargeCoroutine != null)
            {
                StopCoroutine(chargeCoroutine);
                isCharging = false;
            }
            
            isFiringBeam = false;
            
            // Clean up standard beam renderer
            if (beamLine != null)
            {
                beamLine.enabled = false;
            }
            
            // Clean up rope physics objects
            if (useRopePhysics && beamRoot != null)
            {
                beamRoot.SetActive(false);
                
                if (trailRenderer != null)
                {
                    trailRenderer.Clear();
                    trailRenderer.enabled = false;
                }
            }
            
            // Clean up visual effects
            if (beamFX != null)
            {
                beamFX.CleanupAllEffects();
            }
            
            // Stop audio
            if (audioSource != null)
            {
                audioSource.loop = false;
                audioSource.Stop();
                
                // Play end sound
                if (beamEndSound != null)
                {
                    audioSource.PlayOneShot(beamEndSound);
                }
            }
        }
        
        public override void Shoot()
        {
            // Toggle beam on/off
            if (!isFiringBeam && CanFireBeam())
            {
                StartBeam();
            }
            else if (isFiringBeam)
            {
                StopBeam();
            }
        }
        
        private void OnDisable()
        {
            // Make sure to stop the beam when weapon is disabled
            if (isFiringBeam)
            {
                StopBeam();
            }
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            // Apply recommended settings for verlet-based beam
            useRopePhysics = true;       // Enable rope physics
            useSmoothCurve = false;      // Disable other beam methods
            
            // PERFECT SETTINGS: Best balance of responsiveness and lag
            ropeStiffness = 0.65f;       // Slightly reduced for more fluid appearance
            simulationIterations = 30;   // Maintain high iterations for stability
            segmentDistance = 0.1f;      // Smaller segments for smoother appearance
            
            // Enhanced appearance
            beamWidth = 0.2f;
            beamSegments = 32;           // Increased for even smoother appearance
            
            // Setting physics variables for perfect effect
            beamDrag = 0.7f;             // Slightly reduced drag for more persistent motion
            beamTipMass = 2.2f;          // Slight increase for more pronounced lag at tip
            beamStiffness = 0.9f;        // Slight reduction for more motion
            
            // Update beam settings if they've changed
            if (beamLine != null)
            {
                beamLine.startWidth = beamWidth;
                beamLine.endWidth = beamWidth * 0.7f;
                UpdateBeamVisibility();
            }
            
            // Update trail renderer if it exists
            if (trailRenderer != null)
            {
                trailRenderer.startWidth = beamWidth;
                trailRenderer.endWidth = beamWidth * 0.5f;
                
                if (hideBeamColor)
                {
                    trailRenderer.startColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0f);
                    trailRenderer.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0f);
                }
                else
                {
                    trailRenderer.startColor = beamColor;
                    trailRenderer.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0.7f);
                }
            }
            
            // Reinitialize if needed (remaining code the same)
            if ((beamPoints.Count != beamSegments && beamLine != null) || 
                (beamJoints != null && beamJoints.Length != beamSegments))
            {
                if (Application.isPlaying && isFiringBeam)
                {
                    StopBeam();
                }
                
                if (useRopePhysics && Application.isPlaying)
                {
                    InitializeRopeBeam();
                }
                else if (!useRopePhysics && beamLine != null)
                {
                    beamLine.positionCount = beamSegments;
                }
            }
        }
        
        // Add this method to initialize the physics-based beam
        private void InitializeRopeBeam()
        {
            // Clean up any existing physics objects
            if (beamRoot != null)
            {
                Destroy(beamRoot);
            }
            
            // Create a root object to hold all beam joints
            beamRoot = new GameObject("BeamRope");
            beamRoot.transform.SetParent(transform);
            
            // Create joints for physics simulation
            beamJoints = new Transform[beamSegments];
            beamRigidbodies = new Rigidbody2D[beamSegments];
            beamSprings = new SpringJoint2D[beamSegments - 1];
            
            // Create the first joint at the weapon's fire point
            GameObject firstJoint = new GameObject("Joint_0");
            firstJoint.transform.SetParent(beamRoot.transform);
            firstJoint.transform.position = firePoint.position;
            beamJoints[0] = firstJoint.transform;
            
            // Add a rigidbody to the first joint but make it kinematic (controlled by the weapon)
            Rigidbody2D rb = firstJoint.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
            beamRigidbodies[0] = rb;
            
            // Create each subsequent joint and connect with springs
            for (int i = 1; i < beamSegments; i++)
            {
                GameObject joint = new GameObject($"Joint_{i}");
                joint.transform.SetParent(beamRoot.transform);
                
                // Position along the initial beam direction
                Vector3 startPos = firePoint.position;
                Vector3 aimDirection = firePoint.right;
                float t = (float)i / (beamSegments - 1);
                joint.transform.position = startPos + aimDirection * beamRange * t;
                beamJoints[i] = joint.transform;
                
                // Add physics components
                rb = joint.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0;
                rb.linearDamping = beamDrag;
                rb.angularDamping = beamDrag * 2;
                
                // Increase mass towards the tip for more dramatic whipping
                float massMultiplier = Mathf.Lerp(1f, beamTipMass, t);
                rb.mass = 0.1f * massMultiplier;
                beamRigidbodies[i] = rb;
                
                // Add a spring joint to connect to the previous joint
                SpringJoint2D spring = joint.AddComponent<SpringJoint2D>();
                spring.connectedBody = beamRigidbodies[i - 1];
                spring.autoConfigureDistance = false;
                spring.distance = beamRange / (beamSegments - 1);
                spring.frequency = beamStiffness * 5;  // Higher frequency = stiffer spring
                spring.dampingRatio = 0.8f;  // Good balance between bounciness and stability
                beamSprings[i - 1] = spring;
            }
            
            // Add a LineRenderer to connect the joints visually
            LineRenderer jointConnection = beamRoot.AddComponent<LineRenderer>();
            jointConnection.positionCount = beamSegments;
            jointConnection.startWidth = beamWidth;
            jointConnection.endWidth = beamWidth * 0.7f;
            jointConnection.useWorldSpace = true;
            jointConnection.numCapVertices = 6;
            jointConnection.numCornerVertices = 6;
            
            // Set material and color
            if (beamFX != null)
            {
                beamFX.SetupBeamMaterial(jointConnection, beamColor);
            }
            else
            {
                Material beamMaterial = new Material(Shader.Find("Sprites/Default"));
                beamMaterial.SetColor("_Color", beamColor);
                jointConnection.material = beamMaterial;
                jointConnection.startColor = beamColor;
                jointConnection.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0.7f);
            }
            
            // Add trail renderer to the last joint
            trailRenderer = beamJoints[beamSegments - 1].gameObject.AddComponent<TrailRenderer>();
            trailRenderer.startWidth = beamWidth;
            trailRenderer.endWidth = beamWidth * 0.5f;
            trailRenderer.time = 0.1f;  // Short trail time for beam appearance
            trailRenderer.minVertexDistance = 0.05f;
            trailRenderer.alignment = LineAlignment.View;
            trailRenderer.numCapVertices = 6;
            trailRenderer.numCornerVertices = 6;
            
            // Create a material specifically for the trail renderer
            Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
            trailMaterial.SetColor("_Color", beamColor);
            
            // If the beamFX is available, try to copy material settings (without direct access)
            if (beamFX != null)
            {
                // This will copy the material settings from the main beam
                if (beamLine != null && beamLine.material != null)
                {
                    trailMaterial = new Material(beamLine.material);
                }
            }
            
            trailRenderer.material = trailMaterial;
            trailRenderer.startColor = beamColor;
            trailRenderer.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0.7f);
            
            // Hide by default
            if (beamRoot != null)
            {
                beamRoot.SetActive(false);
            }
        }
        
        // Replace the UpdateSmoothBeam method with this simplified version
        private void UpdateSmoothBeam(Vector3 startPos, Vector3 endPos, bool hasHit, Vector3 hitNormal)
        {
            // Get basic direction and distance
            Vector3 beamDirection = (endPos - startPos).normalized;
            float beamDistance = Vector3.Distance(startPos, endPos);
            
            // Create a simple side vector for the curve (perpendicular to aim)
            Vector3 sideVector = new Vector3(-beamDirection.y, beamDirection.x, 0).normalized;
            
            // Create a simple curve by adding points along a sine wave
            curvePoints.Clear();
            
            // Add start point
            curvePoints.Add(startPos);
            
            // Add intermediate points with a simple curve
            for (int i = 1; i < beamSegments - 1; i++)
            {
                float t = (float)i / (beamSegments - 1);
                
                // Create a simple sine curve
                float curveAmount = Mathf.Sin(t * Mathf.PI) * beamCurvature;
                
                // Apply a wave effect
                if (useWaveEffect)
                {
                    curveAmount *= (1f + Mathf.Sin(Time.time * waveFrequency) * 0.5f);
                }
                
                // Create the point along a simple curved path
                Vector3 straightPos = Vector3.Lerp(startPos, endPos, t);
                Vector3 curvedPos = straightPos + sideVector * curveAmount;
                
                curvePoints.Add(curvedPos);
            }
            
            // Add end point
            curvePoints.Add(endPos);
            
            // Update the line renderer
            if (beamLine != null)
            {
                beamLine.positionCount = curvePoints.Count;
                
                for (int i = 0; i < curvePoints.Count; i++)
                {
                    beamLine.SetPosition(i, curvePoints[i]);
                }
            }
            
            // Update beam FX with the curve points
            if (beamFX != null)
            {
                Vector3[] pointsArray = curvePoints.ToArray();
                beamFX.UpdateCurvedBeamAnimation(pointsArray);
            }
            
            // Draw debug lines
            for (int i = 1; i < curvePoints.Count; i++)
            {
                Debug.DrawLine(curvePoints[i-1], curvePoints[i], Color.yellow, 0.1f);
            }
        }
        
        // Add this cubic Bezier method for smoother curves
        private Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float oneMinusT = 1f - t;
            float oneMinusT2 = oneMinusT * oneMinusT;
            float oneMinusT3 = oneMinusT2 * oneMinusT;
            float t2 = t * t;
            float t3 = t2 * t;
            
            return oneMinusT3 * p0 + 
                   3f * oneMinusT2 * t * p1 + 
                   3f * oneMinusT * t2 * p2 + 
                   t3 * p3;
        }
        
        // Add method to reset the beam for debugging
        public void ResetBeam()
        {
            if (isFiringBeam)
            {
                StopBeam();
            }
            
            if (useRopePhysics)
            {
                if (beamRoot != null)
                {
                    Destroy(beamRoot);
                    beamRoot = null;
                }
                InitializeRopeBeam();
            }
            else
            {
                InitializeBeam();
            }
        }
    }
}