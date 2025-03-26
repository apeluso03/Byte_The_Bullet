using UnityEngine;
using System.Collections.Generic;
using Weapons.BeamPoints;

namespace Weapons.BeamPhysics
{
    /// <summary>
    /// Implements verlet integration physics for a rope-like beam
    /// </summary>
    public class RopeBeamPhysics : MonoBehaviour, IBeamPhysics
    {
        private BeamWeaponConfig config;
        private Transform firePoint;
        private List<RopePoint> ropePoints = new List<RopePoint>();
        private float segmentLength;
        private Vector3 lastAimPosition;
        private Vector3 fireVelocity = Vector3.zero;
        
        private LineRenderer beamLine;
        
        public void Initialize(BeamWeaponConfig config, Transform firePoint)
        {
            this.config = config;
            this.firePoint = firePoint;
            
            // Set up the LineRenderer
            if (beamLine == null)
            {
                beamLine = GetComponent<LineRenderer>();
                if (beamLine == null)
                {
                    beamLine = gameObject.AddComponent<LineRenderer>();
                }
                beamLine.startWidth = config.beamWidth;
                beamLine.endWidth = config.beamWidth * 0.7f;
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
            
            InitializeRopePoints(firePoint.position, firePoint.right);
        }
        
        private void InitializeRopePoints(Vector3 startPos, Vector3 aimDirection)
        {
            float beamRange = config.beamRange;
            Vector3 endPos = startPos + aimDirection * beamRange;
            
            // Initialize tracking variables
            fireVelocity = Vector3.zero;
            lastAimPosition = endPos;
            
            // Calculate how many segments we need based on segment distance
            int pointCount = Mathf.CeilToInt(beamRange / config.segmentDistance) + 1;
            pointCount = Mathf.Max(pointCount, 30); // Minimum for smoother curve
            
            segmentLength = beamRange / (pointCount - 1);
            
            // Initialize rope points with an initial curve
            ropePoints.Clear();
            
            // Create points with slight initial curve
            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / (pointCount - 1);
                
                // Create a base position along the beam
                Vector3 basePos = startPos + aimDirection * beamRange * t;
                
                // Add a slight initial curve for more natural appearance
                if (i > 1)
                {
                    Vector3 perpDir = new Vector3(-aimDirection.y, aimDirection.x, 0);
                    float sinCurve = Mathf.Sin(t * Mathf.PI);
                    float curveAmt = sinCurve * 0.05f * beamRange; // Very subtle initial curve
                    basePos += perpDir * curveAmt;
                }
                
                // First point is locked to gun, others are free
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
            
            // Configure the LineRenderer
            if (beamLine != null)
            {
                beamLine.positionCount = ropePoints.Count;
                beamLine.enabled = true;
            }
        }
        
        public void UpdateBeam(Vector3 startPos, Vector3 endPos, Vector3 fireDirection)
        {
            if (ropePoints.Count < 2) return;
            
            float dt = Time.deltaTime;
            
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
            
            // Lock the last point to the target position
            ropePoints[ropePoints.Count - 1].Position = endPos;
            ropePoints[ropePoints.Count - 1].OldPosition = endPos;
            
            // Special handling for second point - creates a small straight section
            if (ropePoints.Count > 1)
            {
                Vector3 secondPointPos = startPos + fireDirection * segmentLength * 1.2f;
                ropePoints[1].Position = secondPointPos;
                ropePoints[1].OldPosition = Vector3.Lerp(ropePoints[1].OldPosition, secondPointPos, dt * 15f);
            }
            
            // Create an ideal curve for the beam to follow
            Vector3[] idealPoints = new Vector3[ropePoints.Count];
            idealPoints[0] = startPos;
            idealPoints[ropePoints.Count - 1] = endPos;
            
            // Calculate the length of the beam
            float totalLength = segmentLength * (ropePoints.Count - 1);
            
            // Calculate perpendicular vector for whip motion
            Vector3 perpendicular = new Vector3(-fireDirection.y, fireDirection.x, 0).normalized;
            
            // Direction to curve (based on aim movement)
            float curveSide = Mathf.Sign(Vector3.Dot(aimVelocity, perpendicular));
            perpendicular *= curveSide;
            
            // Create the ideal curve with strong whip towards the end (skip first and last points)
            for (int i = 2; i < ropePoints.Count - 1; i++)
            {
                float t = (float)i / (ropePoints.Count - 1);
                
                // Base straight position
                Vector3 straightPos = startPos + fireDirection * (t * totalLength);
                
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
                
                // Preserve more of the old velocity
                velocity *= 0.98f;
                
                // Progressively slower follow from gun to tip
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
            for (int iteration = 0; iteration < config.simulationIterations; iteration++)
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
                    float smoothFactor = config.ropeStiffness * 0.7f * (1.0f - (i / (float)ropePoints.Count) * 0.5f);
                    ropePoints[i].Position = Vector3.Lerp(curr, smoothPos, smoothFactor);
                }
                
                // Special handling for second-to-last point - gentler smoothing to preserve endpoint
                int preLastIndex = ropePoints.Count - 2;
                if (preLastIndex > 2)
                {
                    Vector3 prev = ropePoints[preLastIndex - 1].Position;
                    Vector3 curr = ropePoints[preLastIndex].Position;
                    Vector3 next = endPos; // Use exact endpoint
                    
                    float smoothFactor = config.ropeStiffness * 0.3f;
                    Vector3 smoothPos = (prev + next) * 0.5f;
                    ropePoints[preLastIndex].Position = Vector3.Lerp(curr, smoothPos, smoothFactor);
                }
            }
            
            // Update the line renderer
            if (beamLine != null)
            {
                beamLine.enabled = true;
                for (int i = 0; i < ropePoints.Count; i++)
                {
                    beamLine.SetPosition(i, ropePoints[i].Position);
                }
            }
        }
        
        public Vector3[] GetBeamPositions()
        {
            Vector3[] positions = new Vector3[ropePoints.Count];
            for (int i = 0; i < ropePoints.Count; i++)
            {
                positions[i] = ropePoints[i].Position;
            }
            return positions;
        }
        
        public Vector3 GetEndPosition()
        {
            if (ropePoints.Count > 0)
            {
                return ropePoints[ropePoints.Count - 1].Position;
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