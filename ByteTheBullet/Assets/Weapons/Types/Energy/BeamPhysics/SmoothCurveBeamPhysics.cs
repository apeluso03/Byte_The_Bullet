using UnityEngine;
using System.Collections.Generic;
using Weapons.BeamPoints;

namespace Weapons.BeamPhysics
{
    /// <summary>
    /// Implements a smooth curve interpolation for the beam
    /// </summary>
    public class SmoothCurveBeamPhysics : MonoBehaviour, IBeamPhysics
    {
        private BeamWeaponConfig config;
        private Transform firePoint;
        private List<BeamPoint> beamPoints = new List<BeamPoint>();
        private LineRenderer beamLine;
        private List<Vector3> curvePoints = new List<Vector3>();
        
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
            
            // Initialize the beam points
            InitializeBeamPoints();
        }
        
        private void InitializeBeamPoints()
        {
            beamPoints.Clear();
            
            // Default positions - will be updated when fired
            for (int i = 0; i < config.beamSegments; i++)
            {
                float t = (float)i / (config.beamSegments - 1);
                
                // Points further along the beam react more slowly (higher whip effect)
                float followMultiplier = Mathf.Pow(1f - t, config.tipLagMultiplier);
                float pointFollowSpeed = config.beamFollowSpeed * followMultiplier;
                
                beamPoints.Add(new BeamPoint(Vector3.zero, pointFollowSpeed));
            }
        }
        
        public void UpdateBeam(Vector3 startPos, Vector3 endPos, Vector3 fireDirection)
        {
            // Get basic direction and distance
            Vector3 beamDirection = (endPos - startPos).normalized;
            
            // Create a side vector for the curve (perpendicular to aim)
            Vector3 sideVector = new Vector3(-beamDirection.y, beamDirection.x, 0).normalized;
            
            // Update beam points targets
            for (int i = 0; i < beamPoints.Count; i++)
            {
                float t = (float)i / (beamPoints.Count - 1);
                
                // Base position along straight line
                Vector3 straightPos = Vector3.Lerp(startPos, endPos, t);
                
                // Apply wave effect if enabled
                Vector3 finalPos = straightPos;
                if (config.useWaveEffect)
                {
                    // Create a wave motion
                    float waveOffset = Mathf.Sin(t * Mathf.PI + Time.time * config.waveFrequency) * config.waveAmplitude;
                    
                    // Scale wave effect based on distance from start (stronger in middle)
                    float scale = Mathf.Sin(t * Mathf.PI);
                    waveOffset *= scale;
                    
                    // Apply the wave offset
                    finalPos += sideVector * waveOffset * config.beamWidth;
                }
                
                // First and last points are always at start/end positions
                if (i == 0)
                {
                    beamPoints[i].Position = startPos;
                    beamPoints[i].Target = startPos;
                }
                else if (i == beamPoints.Count - 1)
                {
                    beamPoints[i].Position = endPos;
                    beamPoints[i].Target = endPos;
                }
                else
                {
                    // Middle points follow their targets
                    beamPoints[i].Target = finalPos;
                    beamPoints[i].UpdatePosition(Time.deltaTime);
                }
            }
            
            // Apply additional smoothing if enabled
            if (config.useEnhancedSmoothing)
            {
                for (int pass = 0; pass < config.smoothingPasses; pass++)
                {
                    // Skip first and last points since they're anchored
                    for (int i = 1; i < beamPoints.Count - 1; i++)
                    {
                        Vector3 prev = beamPoints[i - 1].Position;
                        Vector3 curr = beamPoints[i].Position;
                        Vector3 next = beamPoints[i + 1].Position;
                        
                        // Simple averaging for smoothing
                        Vector3 avgPos = (prev + next) / 2f;
                        beamPoints[i].Position = Vector3.Lerp(curr, avgPos, config.smoothingStrength);
                    }
                }
            }
            
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
        
        // Helper method for cubic Bezier curve interpolation
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
    }
}