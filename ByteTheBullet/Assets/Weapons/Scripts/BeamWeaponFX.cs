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
            
            // Position and rotate each active section
            for (int i = 0; i < sectionsNeeded; i++)
            {
                GameObject section = beamMiddleInstances[i];
                if (section == null) continue;
                
                // Calculate position - place sections with overlap
                float distanceFromStart = i * effectiveDistance;
                
                // Don't let the last section extend beyond the endpoint
                if (i == sectionsNeeded - 1)
                {
                    // The last section might need special positioning
                    float remainingDistance = beamLength - (i * effectiveDistance);
                    if (remainingDistance < beamSectionDistance)
                    {
                        // Scale the last section to fit exactly
                        float scaleRatio = remainingDistance / beamSectionDistance;
                        section.transform.localScale = new Vector3(scaleRatio, 1f, 1f);
                    }
                }
                
                // Apply position and rotation
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
            
            // Place sections along the curve
            float distanceTraveled = 0;
            int currentPoint = 0;
            Vector3 currentPos = curvePoints[0];
            Vector3 nextPos = curvePoints[1];
            float segmentLength = Vector3.Distance(currentPos, nextPos);
            float segmentTraveled = 0;
            
            for (int i = 0; i < sectionsNeeded; i++)
            {
                GameObject section = beamMiddleInstances[i];
                if (section == null) continue;
                
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
                    
                    // Calculate direction for rotation
                    Vector3 direction = (nextPos - currentPos).normalized;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    Quaternion rotation = Quaternion.Euler(0, 0, angle);
                    
                    // Apply position and rotation
                    section.transform.position = position;
                    section.transform.rotation = rotation;
                }
            }
        }
    }
} 