using UnityEngine;
using System.Collections.Generic;

public class AreaVFXManager : MonoBehaviour
{
    [System.Serializable]
    public class VFXZone
    {
        [Tooltip("Name for this VFX zone")]
        public string zoneName = "Dust Area";
        
        [Tooltip("VFX prefab to spawn (Blizzard, Dust Particles, Falling Snow, etc.)")]
        public GameObject vfxPrefab;
        
        [Tooltip("Center position of this zone (world coordinates)")]
        public Vector3 zoneCenter;
        
        [Tooltip("Radius of the zone (meters)")]
        [Range(10f, 500f)]
        public float zoneRadius = 100f;
        
        [Tooltip("Minimum altitude to activate (leave 0 for no altitude check)")]
        public float minAltitude = 0f;
        
        [Tooltip("Maximum altitude to activate (leave 0 for no altitude check)")]
        public float maxAltitude = 0f;
        
        [Tooltip("VFX follows player within zone")]
        public bool followPlayer = true;
        
        [Tooltip("Height offset above player")]
        [Range(-10f, 50f)]
        public float heightOffset = 10f;
        
        [HideInInspector]
        public GameObject activeVFX;
    }
    
    [Header("Player Reference")]
    [Tooltip("Player transform to track")]
    public Transform player;
    
    [Header("VFX Zones")]
    [Tooltip("Define VFX zones with positions and effects")]
    public List<VFXZone> vfxZones = new List<VFXZone>();
    
    [Header("Global Settings")]
    [Tooltip("Check for zone updates every X seconds")]
    [Range(0.1f, 2f)]
    public float updateInterval = 0.5f;
    
    [Tooltip("Smooth transition distance (meters from zone edge)")]
    [Range(0f, 50f)]
    public float fadeDistance = 20f;
    
    [Header("Debug")]
    [Tooltip("Show debug logs")]
    public bool debugMode = false;
    
    [Tooltip("Show zone gizmos in Scene view")]
    public bool showGizmos = true;
    
    private float lastUpdateTime;
    private GameObject vfxContainer;
    
    void Start()
    {
        // Auto-find player
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                var controller = FindObjectOfType<KinematicCharacterController.Examples.ExampleCharacterController>();
                if (controller != null)
                    player = controller.transform;
            }
            
            if (player == null)
            {
                Debug.LogError("[AreaVFXManager] No player found!");
                enabled = false;
                return;
            }
        }
        
        // Create container
        vfxContainer = new GameObject("VFX_Container");
        vfxContainer.transform.SetParent(transform);
        
        if (debugMode)
        {
            Debug.Log($"[AreaVFXManager] Initialized with {vfxZones.Count} VFX zones");
        }
    }
    
    void Update()
    {
        if (Time.time - lastUpdateTime < updateInterval)
            return;
        
        lastUpdateTime = Time.time;
        
        UpdateVFXZones();
    }
    
    void UpdateVFXZones()
    {
        if (player == null)
            return;
        
        foreach (var zone in vfxZones)
        {
            if (zone.vfxPrefab == null)
                continue;
            
            bool shouldBeActive = IsPlayerInZone(zone);
            bool isCurrentlyActive = zone.activeVFX != null && zone.activeVFX.activeSelf;
            
            if (shouldBeActive && !isCurrentlyActive)
            {
                // Activate VFX
                ActivateVFX(zone);
            }
            else if (!shouldBeActive && isCurrentlyActive)
            {
                // Deactivate VFX
                DeactivateVFX(zone);
            }
            else if (shouldBeActive && isCurrentlyActive && zone.followPlayer)
            {
                // Update VFX position to follow player
                UpdateVFXPosition(zone);
            }
        }
    }
    
    bool IsPlayerInZone(VFXZone zone)
    {
        // Check horizontal distance
        Vector3 playerPos = player.position;
        Vector3 zonePos = zone.zoneCenter;
        
        float horizontalDistance = Vector2.Distance(
            new Vector2(playerPos.x, playerPos.z),
            new Vector2(zonePos.x, zonePos.z)
        );
        
        if (horizontalDistance > zone.zoneRadius)
            return false;
        
        // Check altitude if specified
        if (zone.maxAltitude > 0 && zone.minAltitude < zone.maxAltitude)
        {
            if (playerPos.y < zone.minAltitude || playerPos.y > zone.maxAltitude)
                return false;
        }
        
        return true;
    }
    
    void ActivateVFX(VFXZone zone)
    {
        if (zone.activeVFX == null)
        {
            // Instantiate VFX
            zone.activeVFX = Instantiate(zone.vfxPrefab, vfxContainer.transform);
        }
        
        UpdateVFXPosition(zone);
        zone.activeVFX.SetActive(true);
        
        // Fade in particles if within fade distance
        float distanceFromEdge = zone.zoneRadius - Vector2.Distance(
            new Vector2(player.position.x, player.position.z),
            new Vector2(zone.zoneCenter.x, zone.zoneCenter.z)
        );
        
        if (fadeDistance > 0 && distanceFromEdge < fadeDistance)
        {
            float fadeAlpha = Mathf.Clamp01(distanceFromEdge / fadeDistance);
            SetVFXIntensity(zone.activeVFX, fadeAlpha);
        }
        else
        {
            SetVFXIntensity(zone.activeVFX, 1f);
        }
        
        if (debugMode)
        {
            Debug.Log($"[AreaVFXManager] Activated VFX: {zone.zoneName}");
        }
    }
    
    void DeactivateVFX(VFXZone zone)
    {
        if (zone.activeVFX != null)
        {
            zone.activeVFX.SetActive(false);
            
            if (debugMode)
            {
                Debug.Log($"[AreaVFXManager] Deactivated VFX: {zone.zoneName}");
            }
        }
    }
    
    void UpdateVFXPosition(VFXZone zone)
    {
        if (zone.activeVFX == null)
            return;
        
        if (zone.followPlayer)
        {
            // Position VFX above player
            Vector3 vfxPos = player.position;
            vfxPos.y += zone.heightOffset;
            zone.activeVFX.transform.position = vfxPos;
        }
        else
        {
            // Position VFX at zone center
            zone.activeVFX.transform.position = zone.zoneCenter;
        }
    }
    
    void SetVFXIntensity(GameObject vfx, float intensity)
    {
        // Adjust particle emission rate based on intensity
        ParticleSystem[] particleSystems = vfx.GetComponentsInChildren<ParticleSystem>();
        
        foreach (var ps in particleSystems)
        {
            var emission = ps.emission;
            var rateOverTime = emission.rateOverTime;
            
            // Store original rate if not already stored
            if (!ps.name.Contains("_OriginalRate_"))
            {
                float storedRate = rateOverTime.constant;
                ps.name = ps.name + $"_OriginalRate_{storedRate}";
            }
            
            // Extract original rate from name
            string[] parts = ps.name.Split(new[] { "_OriginalRate_" }, System.StringSplitOptions.None);
            if (parts.Length > 1 && float.TryParse(parts[1], out float originalRate))
            {
                rateOverTime.constant = originalRate * intensity;
                emission.rateOverTime = rateOverTime;
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;
        
        foreach (var zone in vfxZones)
        {
            // Draw zone sphere
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(zone.zoneCenter, zone.zoneRadius);
            
            // Draw fade distance
            if (fadeDistance > 0)
            {
                Gizmos.color = new Color(0, 1, 1, 0.1f);
                Gizmos.DrawWireSphere(zone.zoneCenter, zone.zoneRadius - fadeDistance);
            }
            
            // Draw altitude range if specified
            if (zone.maxAltitude > 0 && zone.minAltitude < zone.maxAltitude)
            {
                Gizmos.color = new Color(1, 1, 0, 0.2f);
                Vector3 center = zone.zoneCenter;
                center.y = (zone.minAltitude + zone.maxAltitude) / 2f;
                float height = zone.maxAltitude - zone.minAltitude;
                Gizmos.DrawWireCube(center, new Vector3(zone.zoneRadius * 2, height, zone.zoneRadius * 2));
            }
            
            // Label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(zone.zoneCenter + Vector3.up * 5f, zone.zoneName);
            #endif
        }
    }
    
    public void AddVFXZone(string name, GameObject vfxPrefab, Vector3 center, float radius)
    {
        VFXZone newZone = new VFXZone
        {
            zoneName = name,
            vfxPrefab = vfxPrefab,
            zoneCenter = center,
            zoneRadius = radius,
            followPlayer = true,
            heightOffset = 10f
        };
        
        vfxZones.Add(newZone);
        
        if (debugMode)
        {
            Debug.Log($"[AreaVFXManager] Added new VFX zone: {name} at {center}");
        }
    }
    
    public void ClearZones()
    {
        foreach (var zone in vfxZones)
        {
            if (zone.activeVFX != null)
            {
                Destroy(zone.activeVFX);
            }
        }
        
        vfxZones.Clear();
    }
    
    void OnDestroy()
    {
        // Clean up active VFX
        foreach (var zone in vfxZones)
        {
            if (zone.activeVFX != null)
            {
                Destroy(zone.activeVFX);
            }
        }
    }
}
