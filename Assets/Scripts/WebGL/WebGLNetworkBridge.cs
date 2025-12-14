using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WebGLNetworkBridge : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WebGLFetchJSON(string url, string gameObjectName, string callbackMethod, string errorMethod);

    [DllImport("__Internal")]
    private static extern void WebGLFetchTexture(string url, string gameObjectName, string callbackMethod, string errorMethod);

    [DllImport("__Internal")]
    private static extern int IsWebGLBuild();
#endif

    // Singleton instance
    private static WebGLNetworkBridge instance;
    public static WebGLNetworkBridge Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("WebGLNetworkBridge");
                instance = go.AddComponent<WebGLNetworkBridge>();
                DontDestroyOnLoad(go);
                Debug.Log("[WebGLNetworkBridge] Singleton instance created");
            }
            return instance;
        }
    }

    // Callbacks for JSON requests
    private Action<string> jsonSuccessCallback;
    private Action<string> jsonErrorCallback;

    // Callbacks for texture requests
    private Action<string> textureSuccessCallback; // Receives base64 string
    private Action<string> textureErrorCallback;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void FetchJSON(string url, Action<string> onSuccess, Action<string> onError)
    {
        Debug.Log($"[WebGLNetworkBridge] FetchJSON called for: {url}");

#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("[WebGLNetworkBridge] Using WebGL JavaScript bridge");

        // Store callbacks
        jsonSuccessCallback = onSuccess;
        jsonErrorCallback = onError;

        try
        {
            // Call JavaScript function
            WebGLFetchJSON(url, gameObject.name, "OnJSONSuccess", "OnJSONError");
            Debug.Log("[WebGLNetworkBridge] JavaScript fetch initiated");
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGLNetworkBridge] Failed to call JavaScript: {e.Message}");
            onError?.Invoke($"Failed to initiate fetch: {e.Message}");
        }
#else
        Debug.Log("[WebGLNetworkBridge] Using Unity Editor fallback (UnityWebRequest)");
        // Fallback for editor: use UnityWebRequest
        StartCoroutine(FetchJSONFallback(url, onSuccess, onError));
#endif
    }

    public void FetchTexture(string url, Action<string> onSuccess, Action<string> onError)
    {
        Debug.Log($"[WebGLNetworkBridge] FetchTexture called for: {url}");

#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("[WebGLNetworkBridge] Using WebGL JavaScript bridge for texture");

        // Store callbacks
        textureSuccessCallback = onSuccess;
        textureErrorCallback = onError;

        try
        {
            // Call JavaScript function
            WebGLFetchTexture(url, gameObject.name, "OnTextureSuccess", "OnTextureError");
            Debug.Log("[WebGLNetworkBridge] JavaScript texture fetch initiated");
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGLNetworkBridge] Failed to call JavaScript for texture: {e.Message}");
            onError?.Invoke($"Failed to initiate texture fetch: {e.Message}");
        }
#else
        Debug.Log("[WebGLNetworkBridge] Using Unity Editor fallback for texture");
        StartCoroutine(FetchTextureFallback(url, onSuccess, onError));
#endif
    }

    // These methods are called by JavaScript
    private void OnJSONSuccess(string data)
    {
        Debug.Log($"[WebGLNetworkBridge] OnJSONSuccess called, data length: {data.Length}");
        jsonSuccessCallback?.Invoke(data);
        jsonSuccessCallback = null;
        jsonErrorCallback = null;
    }

    private void OnJSONError(string error)
    {
        Debug.LogError($"[WebGLNetworkBridge] OnJSONError called: {error}");
        jsonErrorCallback?.Invoke(error);
        jsonSuccessCallback = null;
        jsonErrorCallback = null;
    }

    private void OnTextureSuccess(string base64Data)
    {
        Debug.Log($"[WebGLNetworkBridge] OnTextureSuccess called, data length: {base64Data.Length}");
        textureSuccessCallback?.Invoke(base64Data);
        textureSuccessCallback = null;
        textureErrorCallback = null;
    }

    private void OnTextureError(string error)
    {
        Debug.LogError($"[WebGLNetworkBridge] OnTextureError called: {error}");
        textureErrorCallback?.Invoke(error);
        textureSuccessCallback = null;
        textureErrorCallback = null;
    }

    // Fallback for Editor testing
    private System.Collections.IEnumerator FetchJSONFallback(string url, Action<string> onSuccess, Action<string> onError)
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log($"[WebGLNetworkBridge] Fallback fetch success");
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"[WebGLNetworkBridge] Fallback fetch failed: {request.error}");
                onError?.Invoke(request.error);
            }
        }
    }

    private System.Collections.IEnumerator FetchTextureFallback(string url, Action<string> onSuccess, Action<string> onError)
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log($"[WebGLNetworkBridge] Fallback texture fetch success");

                // Convert texture to base64
                Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                byte[] bytes = texture.EncodeToPNG();
                string base64 = "data:image/png;base64," + Convert.ToBase64String(bytes);

                onSuccess?.Invoke(base64);
            }
            else
            {
                Debug.LogError($"[WebGLNetworkBridge] Fallback texture fetch failed: {request.error}");
                onError?.Invoke(request.error);
            }
        }
    }

    public static Texture2D Base64ToTexture(string base64Data)
    {
        try
        {
            // Remove the data URL prefix if present
            string base64 = base64Data;
            if (base64Data.Contains(","))
            {
                base64 = base64Data.Split(',')[1];
            }

            byte[] imageBytes = Convert.FromBase64String(base64);

            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageBytes);

            Debug.Log($"[WebGLNetworkBridge] Converted base64 to texture: {texture.width}x{texture.height}");

            return texture;
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGLNetworkBridge] Failed to convert base64 to texture: {e.Message}");
            return null;
        }
    }
}
