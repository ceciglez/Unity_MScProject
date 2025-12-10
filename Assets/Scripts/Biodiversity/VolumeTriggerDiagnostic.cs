using UnityEngine;

/// <summary>
/// Diagnostic component to detect when the camera enters a post-processing volume
/// Helps debug why volumes might not be affecting saturation
/// </summary>
public class VolumeTriggerDiagnostic : MonoBehaviour
{
    public string volumeName;
    public float saturation;

    private bool cameraInside = false;
    private static int totalCameraEnters = 0;

    void OnTriggerEnter(Collider other)
    {
        // Check if it's the main camera
        if (other.gameObject.CompareTag("MainCamera") || other.GetComponent<Camera>() != null)
        {
            cameraInside = true;
            totalCameraEnters++;

            Debug.Log($"[VolumeTriggerDiagnostic] 🎥 CAMERA ENTERED VOLUME: {volumeName}");
            Debug.Log($"[VolumeTriggerDiagnostic]   Saturation: {saturation:F2}");
            Debug.Log($"[VolumeTriggerDiagnostic]   Volume Position: {transform.position}");
            Debug.Log($"[VolumeTriggerDiagnostic]   Camera Position: {other.transform.position}");
            Debug.Log($"[VolumeTriggerDiagnostic]   Total camera enters so far: {totalCameraEnters}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if it's the main camera
        if (other.gameObject.CompareTag("MainCamera") || other.GetComponent<Camera>() != null)
        {
            cameraInside = false;
            Debug.Log($"[VolumeTriggerDiagnostic] 🚪 Camera exited volume: {volumeName}");
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Check if it's the main camera - log periodically
        if ((other.gameObject.CompareTag("MainCamera") || other.GetComponent<Camera>() != null) && Time.frameCount % 120 == 0)
        {
            Debug.Log($"[VolumeTriggerDiagnostic] 📍 Camera still inside: {volumeName} (Saturation: {saturation:F2})");
        }
    }
}
