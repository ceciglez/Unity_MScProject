using UnityEngine;

/// <summary>
/// Controls proximity-based audio playback for bird observations
/// </summary>
public class BirdAudioController : MonoBehaviour
{
    private AudioSource audioSource;
    private Transform playerTransform;
    private float triggerDistance;
    private bool hasPlayedRecently = false;
    private float lastPlayTime = 0f;
    private float minTimeBetweenPlays = 30f; // Minimum seconds between plays
    
    public void Initialize(float distance)
    {
        triggerDistance = distance;
        audioSource = GetComponent<AudioSource>();
        
        // Find player transform (assuming it has tag "Player")
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            // Fallback: find main camera
            playerTransform = Camera.main?.transform;
        }
        
        if (playerTransform == null)
        {
            Debug.LogWarning("[BirdAudio] Could not find player transform for bird audio controller");
        }
    }
    
    void Update()
    {
        if (playerTransform == null || audioSource == null || audioSource.clip == null)
            return;
            
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        
        // Check if player is within trigger distance and audio isn't playing
        if (distance <= triggerDistance && !audioSource.isPlaying && !hasPlayedRecently)
        {
            // Random chance to play (to avoid all birds playing at once)
            if (Random.Range(0f, 1f) < 0.3f) // 30% chance per frame when in range
            {
                PlayBirdSound();
            }
        }
        
        // Reset cooldown
        if (hasPlayedRecently && Time.time - lastPlayTime > minTimeBetweenPlays)
        {
            hasPlayedRecently = false;
        }
    }
    
    private void PlayBirdSound()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
            hasPlayedRecently = true;
            lastPlayTime = Time.time;
            
            Debug.Log($"[BirdAudio] Playing bird sound: {audioSource.clip.name}");
        }
    }
    
    // Public method to manually trigger sound
    public void TriggerSound()
    {
        if (!hasPlayedRecently)
        {
            PlayBirdSound();
        }
    }
}