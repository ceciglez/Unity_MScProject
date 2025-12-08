/**
 * WebGL JavaScript Plugin for API Calls
 *
 * This plugin bypasses Unity's UnityWebRequest limitations in WebGL
 * by using native browser Fetch API, which handles CORS properly.
 *
 * Unity's UnityWebRequest in WebGL has known issues with:
 * - CORS preflight requests
 * - Custom headers triggering unnecessary preflight
 * - Certificate validation
 * - Redirect handling
 *
 * This plugin solves all those issues by using the browser's native networking.
 */

mergeInto(LibraryManager.library, {

    /**
     * Fetch JSON data from a URL using browser's native Fetch API
     *
     * @param {string} url - The URL to fetch from
     * @param {string} gameObjectName - Unity GameObject to receive callback
     * @param {string} callbackMethod - Method name to call on success
     * @param {string} errorMethod - Method name to call on error
     */
    WebGLFetchJSON: function(url, gameObjectName, callbackMethod, errorMethod) {
        var urlStr = UTF8ToString(url);
        var gameObjStr = UTF8ToString(gameObjectName);
        var callbackStr = UTF8ToString(callbackMethod);
        var errorStr = UTF8ToString(errorMethod);

        console.log('[WebGLNetworking] Fetching JSON from:', urlStr);
        console.log('[WebGLNetworking] Callback object:', gameObjStr);

        fetch(urlStr, {
            method: 'GET',
            mode: 'cors', // Enable CORS
            cache: 'default',
            credentials: 'omit' // Don't send cookies
        })
        .then(function(response) {
            console.log('[WebGLNetworking] Response status:', response.status);

            if (!response.ok) {
                throw new Error('HTTP ' + response.status + ': ' + response.statusText);
            }

            return response.text();
        })
        .then(function(data) {
            console.log('[WebGLNetworking] Success! Data length:', data.length);
            console.log('[WebGLNetworking] First 200 chars:', data.substring(0, 200));

            // Send data back to Unity
            try {
                SendMessage(gameObjStr, callbackStr, data);
                console.log('[WebGLNetworking] Callback sent successfully');
            } catch (e) {
                console.error('[WebGLNetworking] Failed to send callback:', e);
            }
        })
        .catch(function(error) {
            console.error('[WebGLNetworking] Fetch failed:', error);
            console.error('[WebGLNetworking] Error details:', {
                message: error.message,
                name: error.name,
                stack: error.stack
            });

            var errorMsg = 'WebGL Fetch Error: ' + error.message;

            // Send error back to Unity
            try {
                SendMessage(gameObjStr, errorStr, errorMsg);
                console.log('[WebGLNetworking] Error callback sent');
            } catch (e) {
                console.error('[WebGLNetworking] Failed to send error callback:', e);
            }
        });
    },

    /**
     * Fetch an image/texture from a URL
     * Returns the image as a base64 data URL
     *
     * @param {string} url - The image URL to fetch
     * @param {string} gameObjectName - Unity GameObject to receive callback
     * @param {string} callbackMethod - Method name to call on success (receives base64 string)
     * @param {string} errorMethod - Method name to call on error
     */
    WebGLFetchTexture: function(url, gameObjectName, callbackMethod, errorMethod) {
        var urlStr = UTF8ToString(url);
        var gameObjStr = UTF8ToString(gameObjectName);
        var callbackStr = UTF8ToString(callbackMethod);
        var errorStr = UTF8ToString(errorMethod);

        console.log('[WebGLNetworking] Fetching texture from:', urlStr);

        fetch(urlStr, {
            method: 'GET',
            mode: 'cors',
            cache: 'default',
            credentials: 'omit'
        })
        .then(function(response) {
            console.log('[WebGLNetworking] Texture response status:', response.status);

            if (!response.ok) {
                throw new Error('HTTP ' + response.status + ': ' + response.statusText);
            }

            return response.blob();
        })
        .then(function(blob) {
            console.log('[WebGLNetworking] Texture blob received, size:', blob.size);

            // Convert blob to base64
            var reader = new FileReader();

            reader.onload = function() {
                var base64data = reader.result;
                console.log('[WebGLNetworking] Texture converted to base64, length:', base64data.length);

                // Send base64 data back to Unity
                try {
                    SendMessage(gameObjStr, callbackStr, base64data);
                    console.log('[WebGLNetworking] Texture callback sent successfully');
                } catch (e) {
                    console.error('[WebGLNetworking] Failed to send texture callback:', e);
                }
            };

            reader.onerror = function(error) {
                console.error('[WebGLNetworking] FileReader error:', error);
                SendMessage(gameObjStr, errorStr, 'Failed to convert image to base64');
            };

            reader.readAsDataURL(blob);
        })
        .catch(function(error) {
            console.error('[WebGLNetworking] Texture fetch failed:', error);

            var errorMsg = 'WebGL Texture Fetch Error: ' + error.message;

            try {
                SendMessage(gameObjStr, errorStr, errorMsg);
            } catch (e) {
                console.error('[WebGLNetworking] Failed to send error callback:', e);
            }
        });
    },

    /**
     * Check if running in WebGL
     * Returns 1 if true, 0 if false
     */
    IsWebGLBuild: function() {
        console.log('[WebGLNetworking] IsWebGLBuild check: true');
        return 1;
    }
});
