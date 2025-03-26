using UnityEngine;
using System.Collections.Generic;
using Weapons.BeamPoints;

namespace Weapons.BeamPhysics
{
    /// <summary>
    /// Implements a fluid water hose-like physics for the beam
    /// </summary>
    public class SmoothCurveBeamPhysics : MonoBehaviour, IBeamPhysics
    {
        private BeamWeaponConfig config;
        private Transform firePoint;
        private List<BeamPoint> beamPoints = new List<BeamPoint>();
        private LineRenderer beamLine;
        private List<Vector3> curvePoints = new List<Vector3>();
        
        // New parameters for water hose effect
        private Vector3 previousAimDirection;
        private Vector3 aimVelocity;
        private float wobbleTime = 0f;
        private float wobbleIntensity = 0.3f;
        private float wobbleSpeed = 1.5f;
        
        public void Initialize(BeamWeaponConfig config, Transform firePoint)
        {
            this.config = config;
            this.firePoint = firePoint;
            
            // Set up the line renderer
            if (beamLine == null)
            {
                beamLine = GetComponent<LineRenderer>();
                if (beamLine == null)
                {
                    beamLine = gameObject.AddComponent<LineRenderer>();
                }
                
                beamLine.startWidth = config.beamWidth;
                beamLine.endWidth = config.beamWidth * 0.7f;
                beamLine.positionCount = config.beamSegments;
                beamLine.useWorldSpace = true;
                beamLine.alignment = LineAlignment.View;
                beamLine.numCapVertices = 6;
                beamLine.numCornerVertices = 6;
                
                // Set up material and colors
                Material beamMaterial = new Material(Shader.Find("Sprites/Default"));
                beamMaterial.SetColor("_Color", config.beamColor);
                beamLine.material = beamMaterial;
                
                UpdateBeamVisibility();
            }
            
            // Apply water hose effect parameters
            wobbleIntensity = config.hoseWobbleIntensity;
            wobbleSpeed = config.hoseWobbleSpeed;
            
            // Initialize beam points with slower follow speeds for water hose effect
            InitializeBeamPoints();
            
            // Initialize aim direction
            previousAimDirection = firePoint.right;
            aimVelocity = Vector3.zero;
        }
        
        private void InitializeBeamPoints()
        {
            beamPoints.Clear();
            
            // Default positions - will be updated when fired
            for (int i = 0; i < config.beamSegments; i++)
            {
                float t = (float)i / (config.beamSegments - 1);
                
                // Base follow speed reduced for water hose effect
                float baseFollowSpeed = config.beamFollowSpeed * 0.6f;
                
                // Points further along the beam react more slowly (higher lag effect)
                // Make this more extreme for water hose effect
                float followMultiplier = Mathf.Pow(1f - t, config.tipLagMultiplier * 1.5f);
                float pointFollowSpeed = baseFollowSpeed * followMultiplier;
                
                // Create the point with significantly slower follow speed
                beamPoints.Add(new BeamPoint(Vector3.zero, pointFollowSpeed));
            }
        }
        
        public void UpdateBeam(Vector3 startPos, Vector3 endPos, Vector3 fireDirection)
        {
            // Apply height offset and forward offset to start position
            Vector3 heightOffset = Vector3.up * config.beamHeightOffset;
            Vector3 forwardOffset = fireDirection * config.beamForwardOffset;
            
            startPos += heightOffset + forwardOffset;
            endPos += heightOffset;
            
            // Calculate changes in aim direction
            Vector3 currentAimDirection = (endPos - startPos).normalized;
            
            // Calculate aim velocity with momentum
            Vector3 aimChange = currentAimDirection - previousAimDirection;
            float deltaTime = Time.deltaTime;
            
            // Smooth dampening for aim velocity (more momentum)
            aimVelocity = Vector3.Lerp(aimVelocity, aimChange / deltaTime, deltaTime * 2f);
            
            // Limit maximum aim speed 
            float maxAimSpeed = 6f;
            if (aimVelocity.magnitude > maxAimSpeed)
            {
                aimVelocity = aimVelocity.normalized * maxAimSpeed;
            }
            
            // Store current aim for next frame
            previousAimDirection = currentAimDirection;
            
            // Update wobble effect
            wobbleTime += deltaTime * wobbleSpeed;
            
            // Calculate wobble intensity based on aim velocity magnitude
            float currentWobbleIntensity = wobbleIntensity * Mathf.Min(aimVelocity.magnitude * 0.5f, 1f);
            
            // Create perpendicular vector for wobble
            Vector3 perpendicular = new Vector3(-currentAimDirection.y, currentAimDirection.x, 0).normalized;
            
            // Segment length
            float beamLength = Vector3.Distance(startPos, endPos);
            float segmentLength = beamLength / (config.beamSegments - 1);
            
            // Define how much of the beam (0-1) should be stable near the gun
            float stiffSectionLength = config.stiffPortionLength;
            
            // Calculate positions for each beam point with water hose physics
            for (int i = 0; i < beamPoints.Count; i++)
            {
                float t = (float)i / (beamPoints.Count - 1);
                
                // First point always at start position
                if (i == 0)
                {
                    beamPoints[i].Position = startPos;
                    beamPoints[i].Target = startPos;
                    continue;
                }
                
                // Base position along the beam (straight line)
                Vector3 basePos = startPos + currentAimDirection * (t * beamLength);
                
                // Determine if this point is in the stiff section near the gun
                bool inStiffSection = t < stiffSectionLength;
                
                // Create a transition factor - smooth transition from stiff to flexible
                float flexFactor = Mathf.Clamp01((t - stiffSectionLength) / (1f - stiffSectionLength));
                
                // Apply effects only outside the stiff section
                Vector3 targetPos = basePos;
                
                if (!inStiffSection)
                {
                    // Apply effects proportional to how far we are from the stiff section
                    
                    // Wobble effect (stronger toward tip)
                    float wobblePhase = wobbleTime + t * 4f; // Phase shift along beam
                    float wobbleFactor = Mathf.Sin(wobblePhase) * currentWobbleIntensity * flexFactor * flexFactor;
                    Vector3 wobbleOffset = perpendicular * wobbleFactor * config.beamWidth * 2f;
                    
                    // Direction lag (stronger toward tip)
                    float directionLagFactor = Mathf.Pow(flexFactor, config.whipIntensity);
                    
                    // Calculate lagged direction
                    Vector3 laggedDirection = Vector3.Slerp(
                        currentAimDirection, 
                        previousAimDirection - aimVelocity * directionLagFactor * 0.5f,
                        directionLagFactor
                    );
                    
                    // Apply the lagged direction 
                    Vector3 lagPos = startPos + Vector3.Slerp(
                        currentAimDirection * (t * beamLength),
                        laggedDirection * (t * beamLength),
                        flexFactor
                    );
                    
                    // Add a curve/sag effect for gravity (more pronounced toward the middle-end)
                    float sagFactor = Mathf.Sin(t * Mathf.PI) * 0.05f * beamLength * flexFactor;
                    Vector3 gravity = Vector3.down * sagFactor;
                    
                    // Combine all effects for final target position
                    targetPos = lagPos + wobbleOffset + gravity;
                }
                
                // Set the target
                beamPoints[i].Target = targetPos;
                
                // Make follow speed dependent on position and aim velocity
                float speedFactor = Mathf.Clamp01(1f - aimVelocity.magnitude * 0.2f);
                
                // Points in stiff section follow very quickly
                if (inStiffSection)
                {
                    // Near-instant following for stiff section
                    beamPoints[i].FollowSpeed = beamPoints[i].FollowSpeed * 5f;
                }
                else
                {
                    // Normal following for flexible section
                    beamPoints[i].FollowSpeed = beamPoints[i].FollowSpeed * speedFactor;
                }
                
                // Update the position
                beamPoints[i].UpdatePosition(deltaTime);
            }
            
            // Apply special constraint to maintain straight line for stiff section
            ApplyStiffSectionConstraint(startPos, currentAimDirection, stiffSectionLength, beamLength);
            
            // Apply regular constraints between adjacent points
            ApplyConstraints(segmentLength);
            
            // Update the line renderer
            if (beamLine != null)
            {
                beamLine.enabled = true;
                
                for (int i = 0; i < beamPoints.Count; i++)
                {
                    beamLine.SetPosition(i, beamPoints[i].Position);
                }
            }
        }
        
        // Add constraints between points to prevent stretching
        private void ApplyConstraints(float segmentLength)
        {
            for (int iteration = 0; iteration < 3; iteration++)
            {
                for (int i = 0; i < beamPoints.Count - 1; i++)
                {
                    Vector3 p1 = beamPoints[i].Position;
                    Vector3 p2 = beamPoints[i + 1].Position;
                    
                    // Calculate the direction and distance between points
                    Vector3 direction = p2 - p1;
                    float distance = direction.magnitude;
                    
                    if (distance > 0.0001f)
                    {
                        // Calculate how much we need to adjust
                        Vector3 correction = direction.normalized * (distance - segmentLength);
                        
                        // First point is anchored at fire point
                        if (i == 0)
                        {
                            beamPoints[i+1].Position -= correction;
                        }
                        else
                        {
                            // Apply correction to both points (except first point)
                            beamPoints[i].Position += correction * 0.5f;
                            beamPoints[i+1].Position -= correction * 0.5f;
                        }
                    }
                }
            }
        }
        
        // Add this new method to enforce a stiff, straight section near the gun
        private void ApplyStiffSectionConstraint(Vector3 startPos, Vector3 aimDirection, float stiffPortion, float beamLength)
        {
            // Calculate how many points are in the stiff section
            int stiffPointCount = Mathf.CeilToInt(beamPoints.Count * stiffPortion);
            
            // Ensure at least 2 points in stiff section for stability
            stiffPointCount = Mathf.Max(stiffPointCount, 2);
            
            // Force these points to form a straight line from the gun
            for (int i = 0; i < stiffPointCount; i++)
            {
                float t = (float)i / (beamPoints.Count - 1);
                float distance = t * beamLength;
                
                // Calculate the position directly on the aim line
                Vector3 straightPos = startPos + aimDirection * distance;
                
                // Force this position (completely rigid)
                beamPoints[i].Position = straightPos;
                beamPoints[i].Target = straightPos;
            }
        }
        
        public Vector3[] GetBeamPositions()
        {
            Vector3[] positions = new Vector3[beamPoints.Count];
            for (int i = 0; i < beamPoints.Count; i++)
            {
                positions[i] = beamPoints[i].Position;
            }
            return positions;
        }
        
        public Vector3 GetEndPosition()
        {
            if (beamPoints.Count > 0)
            {
                return beamPoints[beamPoints.Count - 1].Position;
            }
            return firePoint.position + firePoint.right * config.beamRange;
        }
        
        public void Cleanup()
        {
            if (beamLine != null)
            {
                beamLine.enabled = false;
            }
        }
        
        public void UpdateBeamVisibility()
        {
            if (beamLine != null)
            {
                if (config.hideBeamColor)
                {
                    beamLine.startColor = new Color(config.beamColor.r, config.beamColor.g, config.beamColor.b, 0f);
                    beamLine.endColor = new Color(config.beamColor.r, config.beamColor.g, config.beamColor.b, 0f);
                }
                else
                {
                    beamLine.startColor = config.beamColor;
                    beamLine.endColor = new Color(config.beamColor.r, config.beamColor.g, config.beamColor.b, 0.7f);
                }
            }
        }
    }
}