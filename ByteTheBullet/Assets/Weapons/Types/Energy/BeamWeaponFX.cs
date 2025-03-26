using UnityEngine;
using System.Collections.Generic;

namespace Weapons
{
    public class BeamWeaponFX : MonoBehaviour
    {
        [Header("Animation Prefabs")]
        public GameObject chargeFXPrefab;
        public GameObject flareFXPrefab;
        public GameObject impactFXPrefab;
        public GameObject beamMiddleAnimPrefab;
        
        [Header("Beam Settings")]
        public float beamWidth = 0.2f;
        
        [Header("Beam Middle Settings")]
        [Space(10)]

        [Tooltip("Distance between repeated beam middle sprites")]
        [Range(0.1f, 5.0f)]
        public float beamSectionDistance = 1.0f;

        [Tooltip("How much sections should overlap (0-1). Higher values close gaps between sections.")]
        [Range(0f, 0.9f)]
        public float sectionOverlap = 0.3f;

        [Tooltip("Enable detailed debugging of beam sections")]
        public bool debugBeamSections = false;
        
        // References to instantiated effects
        private GameObject chargeEffect;
        private GameObject flareStartEffect;
        private GameObject flareEndEffect;
        private GameObject impactEffect;
        private List<GameObject> beamMiddleInstances = new List<GameObject>();
        private Material beamMaterial;
        
        // Add property change detection
        private float _lastBeamWidth;
        private float _lastSectionDistance;
        private float _lastSectionOverlap;
        
        // Setup the beam material
        public void SetupBeamMaterial(LineRenderer lineRenderer, Color beamColor)
        {
            // Create a material for the beam that doesn't affect sprite colors
            beamMaterial = new Material(Shader.Find("Sprites/Default"));
            
            // Don't apply the color tint to the material, just to the line renderer itself
            lineRenderer.startColor = beamColor;
            lineRenderer.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0.5f);
            
            // Apply to line renderer without color tint
            lineRenderer.material = beamMaterial;
            
            // If we have beam middle animation prefab, instantiate it
            if (beamMiddleAnimPrefab != null && beamMiddleInstances.Count == 0)
            {
                GameObject newSection = Instantiate(beamMiddleAnimPrefab);
                beamMiddleInstances.Add(newSection);
            }
        }
        
        // Play charge animation at the muzzle
        public void PlayChargeEffect(Transform firePoint)
        {
            if (chargeFXPrefab != null && chargeEffect == null)
            {
                chargeEffect = Instantiate(chargeFXPrefab, firePoint.position, firePoint.rotation);
                chargeEffect.transform.parent = firePoint;
            }
        }
        
        // Stop charge animation
        public void StopChargeEffect()
        {
            if (chargeEffect != null)
            {
                Destroy(chargeEffect);
                chargeEffect = null;
            }
        }
        
        // Play flare effects at start and end of beam
        public void PlayFlareEffects(Vector3 startPos, Vector3 endPos, Quaternion startRot, Quaternion endRot)
        {
            if (flareFXPrefab != null)
            {
                // Start flare - position it exactly at the start point
                if (flareStartEffect == null)
                {
                    flareStartEffect = Instantiate(flareFXPrefab, startPos, startRot);
                    // Make sure it's directly at the weapon muzzle
                    flareStartEffect.transform.position = startPos;
                }
                else
                {
                    // Update position precisely
                    flareStartEffect.transform.position = startPos;
                    flareStartEffect.transform.rotation = startRot;
                }
                
                // End flare
                if (flareEndEffect == null)
                {
                    flareEndEffect = Instantiate(flareFXPrefab, endPos, endRot);
                    flareEndEffect.transform.position = endPos;
                    // Scale end flare smaller if desired
                    flareEndEffect.transform.localScale = flareStartEffect.transform.localScale * 0.8f;
                }
                else
                {
                    flareEndEffect.transform.position = endPos;
                    flareEndEffect.transform.rotation = endRot;
                }
            }
        }
        
        // Stop flare effects
        public void StopFlareEffects()
        {
            if (flareStartEffect != null)
            {
                Destroy(flareStartEffect);
                flareStartEffect = null;
            }
            
            if (flareEndEffect != null)
            {
                Destroy(flareEndEffect);
                flareEndEffect = null;
            }
        }
        
        // Play impact animation where beam hits
        public void PlayImpactEffect(Vector3 position, Vector3 normal)
        {
            if (impactFXPrefab != null)
            {
                // Create rotation that aligns with the hit normal
                Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, normal);
                
                if (impactEffect == null)
                {
                    impactEffect = Instantiate(impactFXPrefab, position, rotation);
                }
                else
                {
                    impactEffect.transform.position = position;
                    impactEffect.transform.rotation = rotation;
                }
            }
        }
        
        // Stop impact animation
        public void StopImpactEffect()
        {
            if (impactEffect != null)
            {
                Destroy(impactEffect);
                impactEffect = null;
            }
        }
        
        public void UpdateBeamMiddleAnimation(Vector3 startPos, Vector3 endPos)
        {
            // Calculate beam properties
            Vector3 direction = endPos - startPos;
            float beamLength = direction.magnitude;
            Vector3 normalizedDir = direction.normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            
            // Calculate effective section distance (accounting for overlap)
            float effectiveDistance = beamSectionDistance * (1f - sectionOverlap);
            if (effectiveDistance <= 0.05f) effectiveDistance = 0.05f; // Prevent division by zero
            
            // Calculate how many beam sections we need
            int sectionsNeeded = Mathf.CeilToInt(beamLength / effectiveDistance);
            sectionsNeeded = Mathf.Max(sectionsNeeded, 1); // Ensure at least one section
            
            // Create sections if needed
            while (beamMiddleInstances.Count < sectionsNeeded)
            {
                if (beamMiddleAnimPrefab != null)
                {
                    GameObject newSection = Instantiate(beamMiddleAnimPrefab);
                    
                    // Ensure consistent scaling for all sections
                    if (beamMiddleInstances.Count > 0 && beamMiddleInstances[0] != null)
                    {
                        newSection.transform.localScale = beamMiddleInstances[0].transform.localScale;
                    }
                    
                    beamMiddleInstances.Add(newSection);
                }
            }
            
            // Hide extra sections if we have too many
            for (int i = 0; i < beamMiddleInstances.Count; i++)
            {
                if (i < sectionsNeeded)
                {
                    beamMiddleInstances[i].SetActive(true);
                }
                else
                {
                    beamMiddleInstances[i].SetActive(false);
                }
            }
            
            // Get the base size of a beam section to ensure proper scaling and alignment
            float sectionBaseWidth = 1f;
            float sectionBaseLength = 1f;
            
            if (beamMiddleInstances.Count > 0 && beamMiddleInstances[0] != null)
            {
                // Try to get sprite renderer to determine actual dimensions
                SpriteRenderer sr = beamMiddleInstances[0].GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    sectionBaseWidth = sr.sprite.bounds.size.y;
                    sectionBaseLength = sr.sprite.bounds.size.x;
                }
            }
            
            // Apply consistent width scaling to all sections
            float widthScale = beamWidth / sectionBaseWidth;
            
            // Position and rotate each active section
            for (int i = 0; i < sectionsNeeded; i++)
            {
                GameObject section = beamMiddleInstances[i];
                if (section == null) continue;
                
                // Apply consistent width scaling
                Vector3 scale = section.transform.localScale;
                scale.y = widthScale;
                
                // Calculate position - place sections with overlap
                float distanceFromStart = i * effectiveDistance;
                
                // For the last section, calculate exact length needed
                if (i == sectionsNeeded - 1)
                {
                    float remainingDistance = beamLength - (i * effectiveDistance);
                    
                    // Scale the last section to fit exactly
                    if (remainingDistance < beamSectionDistance)
                    {
                        float scaleRatio = remainingDistance / beamSectionDistance;
                        scale.x = scaleRatio;
                    }
                }
                else
                {
                    // For middle sections, ensure they overlap properly by slight length scaling
                    scale.x = 1f + (sectionOverlap * 0.1f); // Slightly increase length for better overlap
                }
                
                // Apply scaling
                section.transform.localScale = scale;
                
                // Apply position and rotation - ensure sections line up precisely
                Vector3 position = startPos + normalizedDir * distanceFromStart;
                section.transform.position = position;
                section.transform.rotation = rotation;
            }
        }
        
        // Clean up all effects
        public void CleanupAllEffects()
        {
            StopChargeEffect();
            StopFlareEffects();
            StopImpactEffect();
            
            // Hide all beam middle sections
            foreach (GameObject section in beamMiddleInstances)
            {
                if (section != null)
                {
                    section.SetActive(false);
                }
            }
        }
        
        // Clean up on destroy
        private void OnDestroy()
        {
            foreach (GameObject section in beamMiddleInstances)
            {
                if (section != null)
                {
                    Destroy(section);
                }
            }
            beamMiddleInstances.Clear();
        }

        private void OnDrawGizmos()
        {
            if (!debugBeamSections || beamMiddleInstances == null) return;
            
            // Draw debug info for active sections
            for (int i = 0; i < beamMiddleInstances.Count; i++)
            {
                if (beamMiddleInstances[i] != null && beamMiddleInstances[i].activeSelf)
                {
                    // Draw section bounds
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f); // Orange
                    Vector3 pos = beamMiddleInstances[i].transform.position;
                    Gizmos.DrawWireSphere(pos, 0.1f);
                    
                    // Draw section number
                    UnityEditor.Handles.Label(pos + Vector3.up * 0.2f, i.ToString());
                }
            }
        }

        public void ResetBeamSections()
        {
            // Destroy all existing sections
            foreach (GameObject section in beamMiddleInstances)
            {
                if (section != null)
                {
                    DestroyImmediate(section);
                }
            }
            beamMiddleInstances.Clear();
        }

        public void UpdateCurvedBeamAnimation(Vector3[] curvePoints)
        {
            if (curvePoints == null || curvePoints.Length < 2) return;
            
            // Calculate how many beam sections we need based on total path length
            float totalPathLength = 0;
            for (int i = 1; i < curvePoints.Length; i++)
            {
                totalPathLength += Vector3.Distance(curvePoints[i], curvePoints[i-1]);
            }
            
            // Calculate effective section distance (accounting for overlap)
            float effectiveDistance = beamSectionDistance * (1f - sectionOverlap);
            if (effectiveDistance <= 0.05f) effectiveDistance = 0.05f;
            
            // Calculate how many sections needed based on total path length
            int sectionsNeeded = Mathf.CeilToInt(totalPathLength / effectiveDistance);
            sectionsNeeded = Mathf.Max(sectionsNeeded, 1);
            
            // Create or update beam sections
            while (beamMiddleInstances.Count < sectionsNeeded)
            {
                if (beamMiddleAnimPrefab != null)
                {
                    GameObject newSection = Instantiate(beamMiddleAnimPrefab);
                    beamMiddleInstances.Add(newSection);
                }
            }
            
            // Hide extra sections
            for (int i = 0; i < beamMiddleInstances.Count; i++)
            {
                if (i < sectionsNeeded)
                {
                    beamMiddleInstances[i].SetActive(true);
                }
                else
                {
                    beamMiddleInstances[i].SetActive(false);
                }
            }
            
            // Get base section dimensions
            float sectionBaseWidth = 1f;
            if (beamMiddleInstances.Count > 0 && beamMiddleInstances[0] != null)
            {
                SpriteRenderer sr = beamMiddleInstances[0].GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    sectionBaseWidth = sr.sprite.bounds.size.y;
                }
            }
            
            // Apply consistent width scaling
            float widthScale = beamWidth / sectionBaseWidth;
            
            // Place sections along the curve with precise alignment
            float distanceTraveled = 0;
            int currentPoint = 0;
            Vector3 currentPos = curvePoints[0];
            Vector3 nextPos = curvePoints[1];
            float segmentLength = Vector3.Distance(currentPos, nextPos);
            float segmentTraveled = 0;
            Vector3 prevPosition = currentPos; // Keep track of previous position for proper alignment
            
            for (int i = 0; i < sectionsNeeded; i++)
            {
                GameObject section = beamMiddleInstances[i];
                if (section == null) continue;
                
                // Apply width scaling
                Vector3 scale = section.transform.localScale;
                scale.y = widthScale;
                section.transform.localScale = scale;
                
                // Calculate target distance for this section
                float targetDistance = i * effectiveDistance;
                
                // Move along curve until we reach the target distance
                while (distanceTraveled < targetDistance && currentPoint < curvePoints.Length - 1)
                {
                    // How much more we need to travel
                    float distanceRemaining = targetDistance - distanceTraveled;
                    
                    // Can we reach it in the current segment?
                    if (segmentTraveled + distanceRemaining < segmentLength)
                    {
                        // We can stay in the current segment
                        segmentTraveled += distanceRemaining;
                        distanceTraveled = targetDistance;
                    }
                    else
                    {
                        // Move to next segment
                        float distanceCovered = segmentLength - segmentTraveled;
                        distanceTraveled += distanceCovered;
                        prevPosition = nextPos; // Store for orientation
                        currentPoint++;
                        
                        if (currentPoint < curvePoints.Length - 1)
                        {
                            currentPos = curvePoints[currentPoint];
                            nextPos = curvePoints[currentPoint + 1];
                            segmentLength = Vector3.Distance(currentPos, nextPos);
                            segmentTraveled = 0;
                        }
                    }
                }
                
                // Calculate position and rotation for this section
                if (currentPoint < curvePoints.Length - 1)
                {
                    // Interpolate position along the current segment
                    float t = segmentTraveled / segmentLength;
                    Vector3 position = Vector3.Lerp(currentPos, nextPos, t);
                    
                    // Calculate direction for rotation - use the exact segment direction
                    Vector3 direction = (nextPos - currentPos).normalized;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    Quaternion rotation = Quaternion.Euler(0, 0, angle);
                    
                    // For better alignment, slightly offset position along the direction
                    if (i > 0 && i < sectionsNeeded - 1)
                    {
                        // Add a tiny bit of overlap between sections
                        position -= direction * (beamSectionDistance * sectionOverlap * 0.5f);
                    }
                    
                    // Apply position and rotation
                    section.transform.position = position;
                    section.transform.rotation = rotation;
                    
                    // Store this position for next section's alignment
                    prevPosition = position;
                }
            }
        }

        private void Update()
        {
            // Check if properties have changed
            if (beamWidth != _lastBeamWidth || 
                beamSectionDistance != _lastSectionDistance || 
                sectionOverlap != _lastSectionOverlap)
            {
                // Values have changed, update the visuals
                if (beamMiddleInstances.Count > 0 && beamMiddleInstances[0] != null && beamMiddleInstances[0].activeSelf)
                {
                    // Get the active beam start and end positions
                    Vector3 startPos = beamMiddleInstances[0].transform.position;
                    Vector3 endPos = startPos;
                    
                    // Find the last active section
                    for (int i = beamMiddleInstances.Count - 1; i >= 0; i--)
                    {
                        if (beamMiddleInstances[i] != null && beamMiddleInstances[i].activeSelf)
                        {
                            endPos = beamMiddleInstances[i].transform.position;
                            break;
                        }
                    }
                    
                    // Calculate direction
                    Vector3 direction = endPos - startPos;
                    
                    // Only update if we have a valid direction
                    if (direction.magnitude > 0.1f)
                    {
                        // Recalculate end position based on beam length
                        endPos = startPos + direction.normalized * direction.magnitude;
                        
                        // Reapply the beam layout with new settings
                        UpdateBeamMiddleAnimation(startPos, endPos);
                    }
                }
                
                // Update line renderers for consistent width
                LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>(true);
                foreach (LineRenderer lr in lineRenderers)
                {
                    lr.startWidth = beamWidth;
                    lr.endWidth = beamWidth * 0.7f;
                }
                
                // Store current values
                _lastBeamWidth = beamWidth;
                _lastSectionDistance = beamSectionDistance;
                _lastSectionOverlap = sectionOverlap;
            }
        }

        // Add this new method that specifically updates only the beam width
        public void UpdateBeamWidth(float width)
        {
            // Store the new width
            beamWidth = width;
            _lastBeamWidth = width;
            
            // Update line renderers
            LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>(true);
            foreach (LineRenderer lr in lineRenderers)
            {
                lr.startWidth = width;
                lr.endWidth = width * 0.7f;
            }
            
            // Update beam middle section scales
            foreach (GameObject section in beamMiddleInstances)
            {
                if (section != null)
                {
                    // Maintain x scale (length) but update y scale (width)
                    Vector3 scale = section.transform.localScale;
                    scale.y = width * 5f; // Multiplier to make it visually appropriate
                    section.transform.localScale = scale;
                    
                    // Also update any child sprite renderers
                    SpriteRenderer[] renderers = section.GetComponentsInChildren<SpriteRenderer>();
                    foreach (SpriteRenderer sr in renderers)
                    {
                        // Update sprite width
                        sr.size = new Vector2(sr.size.x, width * 2f);
                    }
                }
            }
        }
    }
} 