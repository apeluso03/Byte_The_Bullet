using UnityEngine;

namespace Weapons.BeamPhysics
{
    /// <summary>
    /// A simple straight-line beam physics implementation
    /// </summary>
    public class SimpleBeamPhysics : MonoBehaviour, IBeamPhysics
    {
        private BeamWeaponConfig config;
        private Transform firePoint;
        private LineRenderer beamLine;
        private Vector3 startPos;
        private Vector3 endPos;
        
        public void Initialize(BeamWeaponConfig config, Transform firePoint)
        {
            this.config = config;
            this.firePoint = firePoint;
            
            // Set up line renderer
            beamLine = GetComponent<LineRenderer>();
            if (beamLine == null)
            {
                beamLine = gameObject.AddComponent<LineRenderer>();
            }
            
            beamLine.startWidth = config.beamWidth;
            beamLine.endWidth = config.beamWidth * 0.7f;
            beamLine.positionCount = 2; // Just start and end points
            beamLine.useWorldSpace = true;
            
            // Set up material and colors
            Material beamMaterial = new Material(Shader.Find("Sprites/Default"));
            beamMaterial.SetColor("_Color", config.beamColor);
            beamLine.material = beamMaterial;
            
            UpdateBeamVisibility();
        }
        
        public void UpdateBeam(Vector3 startPos, Vector3 endPos, Vector3 fireDirection)
        {
            this.startPos = startPos;
            this.endPos = endPos;
            
            // Simple straight line from start to end
            if (beamLine != null)
            {
                beamLine.enabled = true;
                beamLine.SetPosition(0, startPos);
                beamLine.SetPosition(1, endPos);
            }
        }
        
        public Vector3[] GetBeamPositions()
        {
            return new Vector3[] { startPos, endPos };
        }
        
        public Vector3 GetEndPosition()
        {
            return endPos;
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