using UnityEngine;
using UnityEngine.Networking;

public static class WebGLCorsHelper
{
    public static UnityWebRequest CreateCorsRequest(string url)
    {
        UnityWebRequest request = new UnityWebRequest(url, "GET");
        request.downloadHandler = new DownloadHandlerBuffer();

#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[WebGLCorsHelper] WebGL Build - Creating request for: {url}");

        // CRITICAL: Don't set custom headers in WebGL - they can trigger CORS preflight
        // Let the browser handle headers automatically

        // Set the certificateHandler to bypass SSL certificate validation in WebGL
        // This is safe for WebGL as the browser handles SSL
        request.certificateHandler = new WebGLCertificateHandler();

        // Set a longer timeout for WebGL (60 seconds)
        request.timeout = 60;

        // Disable redirects to avoid CORS issues
        request.redirectLimit = 0;
#else
        Debug.Log($"[WebGLCorsHelper] Editor Mode - Creating standard request for: {url}");
        request.timeout = 30;
#endif

        return request;
    }

    public static UnityWebRequest CreateCorsTextureRequest(string url)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"[WebGLCorsHelper] WebGL Build - Creating texture request for: {url}");

        // In WebGL, create a custom request instead of using UnityWebRequestTexture
        // because UnityWebRequestTexture can have issues with CORS
        UnityWebRequest request = new UnityWebRequest(url, "GET");
        request.downloadHandler = new DownloadHandlerTexture();

        // Set the certificateHandler
        request.certificateHandler = new WebGLCertificateHandler();

        // Longer timeout for images
        request.timeout = 60;

        // Disable redirects
        request.redirectLimit = 0;

        return request;
#else
        Debug.Log($"[WebGLCorsHelper] Editor Mode - Creating texture request for: {url}");
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        request.timeout = 30;
        return request;
#endif
    }

    private class WebGLCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // In WebGL, the browser handles certificate validation
            // Always return true to let the browser do its job
            return true;
        }
    }

    public static void LogRequestError(UnityWebRequest request, string context)
    {
        Debug.LogError($"[WebGLCorsHelper] {context} FAILED");
        Debug.LogError($"  URL: {request.url}");
        Debug.LogError($"  Error: {request.error}");
        Debug.LogError($"  Response Code: {request.responseCode}");
        Debug.LogError($"  Is Network Error: {request.result == UnityWebRequest.Result.ConnectionError}");
        Debug.LogError($"  Is HTTP Error: {request.result == UnityWebRequest.Result.ProtocolError}");

#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.LogError("  [WebGL] This error might be due to:");
        Debug.LogError("    1. CORS policy blocking the request");
        Debug.LogError("    2. Mixed content (HTTP page loading HTTPS resource)");
        Debug.LogError("    3. Ad blocker or browser extension blocking the request");
        Debug.LogError("    4. API rate limiting");
        Debug.LogError("  [WebGL] Check the browser console (F12) for more details!");
#endif

        // Try to get response text if available
        if (!string.IsNullOrEmpty(request.downloadHandler?.text))
        {
            Debug.LogError($"  Response body: {request.downloadHandler.text.Substring(0, Mathf.Min(500, request.downloadHandler.text.Length))}");
        }
    }
}
