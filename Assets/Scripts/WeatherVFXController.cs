using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

/// <summary>
/// Fetches real-world weather data from OpenWeatherMap API based on player coordinates.
/// Automatically triggers appropriate VFX (rain, snow, dust, etc.) based on conditions.
/// </summary>
public class WeatherVFXController : MonoBehaviour
{
    [Header("API Configuration")]
    [Tooltip("Your OpenWeatherMap API key (get free at openweathermap.org/api)")]
    public string apiKey = "YOUR_API_KEY_HERE";
    
    [Tooltip("Update weather data every X seconds")]
    [Range(60f, 3600f)]
    public float updateInterval = 300f; // 5 minutes default
    
    [Header("Player Reference")]
    [Tooltip("Player transform to get coordinates from")]
    public Transform player;
    
    [Header("VFX Prefabs")]
    [Tooltip("VFX for rain/drizzle")]
    public GameObject rainVFX;
    
    [Tooltip("VFX for snow/blizzard")]
    public GameObject snowVFX;
    
    [Tooltip("VFX for dust/mist")]
    public GameObject dustVFX;
    
    [Tooltip("VFX for clear weather particles (optional - leaves, fireflies, etc.)")]
    public GameObject clearVFX;
    
    [Header("VFX Settings")]
    [Tooltip("Height offset above player for weather VFX")]
    [Range(0f, 50f)]
    public float vfxHeightOffset = 15f;
    
    [Tooltip("Temperature threshold for snow (Celsius)")]
    [Range(-10f, 5f)]
    public float snowTemperatureThreshold = 2f;
    
    [Tooltip("Minimum precipitation for VFX activation (mm)")]
    [Range(0f, 5f)]
    public float precipitationThreshold = 0.5f;
    
    [Header("Mapbox Coordinate Conversion")]
    [Tooltip("Reference to AbstractMap for coordinate conversion")]
    public Mapbox.Unity.Map.AbstractMap mapboxMap;
    
    [Header("Debug")]
    [Tooltip("Show debug logs and weather info")]
    public bool debugMode = false;
    
    [Tooltip("Use test coordinates instead of player position")]
    public bool useTestCoordinates = false;
    
    [Tooltip("Test latitude (e.g., 51.5074 for London)")]
    public double testLatitude = 51.5074;
    
    [Tooltip("Test longitude (e.g., -0.1278 for London)")]
    public double testLongitude = -0.1278;
    
    // Weather data
    private WeatherData currentWeather;
    private GameObject activeVFX;
    private float lastUpdateTime;
    private Coroutine weatherUpdateCoroutine;
    
    [Serializable]
    public class WeatherData
    {
        public string condition;        // "Clear", "Clouds", "Rain", "Snow", "Drizzle", "Mist", etc.
        public float temperature;       // Celsius
        public float precipitation;     // mm (rain/snow volume for last hour)
        public float humidity;          // Percentage
        public float windSpeed;         // m/s
        public string description;      // Detailed description
    }
    
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
        }
        
        // Auto-find mapbox map
        if (mapboxMap == null)
        {
            mapboxMap = FindObjectOfType<Mapbox.Unity.Map.AbstractMap>();
        }
        
        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_API_KEY_HERE")
        {
            Debug.LogWarning("[WeatherVFX] No API key set! Get one free at openweathermap.org/api");
            if (!useTestCoordinates)
            {
                enabled = false;
                return;
            }
        }
        
        if (debugMode)
        {
            Debug.Log("[WeatherVFX] Initialized. Update interval: " + updateInterval + "s");
        }
        
        // Fetch initial weather
        FetchWeatherData();
    }
    
    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            FetchWeatherData();
        }
        
        // Update VFX position to follow player
        if (activeVFX != null && player != null)
        {
            Vector3 vfxPos = player.position;
            vfxPos.y += vfxHeightOffset;
            activeVFX.transform.position = vfxPos;
        }
    }
    
    public void FetchWeatherData()
    {
        if (weatherUpdateCoroutine != null)
        {
            StopCoroutine(weatherUpdateCoroutine);
        }
        
        weatherUpdateCoroutine = StartCoroutine(FetchWeatherCoroutine());
    }
    
    IEnumerator FetchWeatherCoroutine()
    {
        lastUpdateTime = Time.time;
        
        // Get coordinates
        double lat, lon;
        
        if (useTestCoordinates)
        {
            lat = testLatitude;
            lon = testLongitude;
            
            if (debugMode)
            {
                Debug.Log($"[WeatherVFX] Using test coordinates: {lat}, {lon}");
            }
        }
        else
        {
            if (player == null || mapboxMap == null)
            {
                Debug.LogError("[WeatherVFX] Player or Mapbox map not found!");
                yield break;
            }
            
            // Convert Unity world position to lat/lon
            var latLon = mapboxMap.WorldToGeoPosition(player.position);
            lat = latLon.x;
            lon = latLon.y;
            
            if (debugMode)
            {
                Debug.Log($"[WeatherVFX] Player coordinates: {lat}, {lon}");
            }
        }
        
        // Build API URL
        string url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={apiKey}&units=metric";
        
        // Make request
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                ParseWeatherData(request.downloadHandler.text);
                UpdateVFX();
            }
            else
            {
                Debug.LogError($"[WeatherVFX] API Error: {request.error}");
                
                if (request.responseCode == 401)
                {
                    Debug.LogError("[WeatherVFX] Invalid API key! Get one at openweathermap.org/api");
                }
            }
        }
        
        weatherUpdateCoroutine = null;
    }
    
    void ParseWeatherData(string json)
    {
        try
        {
            // Parse JSON response
            var data = JsonUtility.FromJson<OpenWeatherResponse>(json);
            
            currentWeather = new WeatherData
            {
                condition = data.weather[0].main,
                description = data.weather[0].description,
                temperature = data.main.temp,
                humidity = data.main.humidity,
                windSpeed = data.wind.speed,
                precipitation = 0f
            };
            
            // Get precipitation (rain or snow volume)
            if (data.rain != null && data.rain.oneHour > 0)
            {
                currentWeather.precipitation = data.rain.oneHour;
            }
            else if (data.snow != null && data.snow.oneHour > 0)
            {
                currentWeather.precipitation = data.snow.oneHour;
            }
            
            if (debugMode)
            {
                Debug.Log($"[WeatherVFX] Weather: {currentWeather.condition} - {currentWeather.description}");
                Debug.Log($"[WeatherVFX] Temp: {currentWeather.temperature}°C, Precip: {currentWeather.precipitation}mm, Humidity: {currentWeather.humidity}%");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WeatherVFX] Failed to parse weather data: {e.Message}");
        }
    }
    
    void UpdateVFX()
    {
        if (currentWeather == null)
            return;
        
        // Determine which VFX to use
        GameObject targetVFX = null;
        
        // Snow conditions: cold temperature + precipitation
        if (currentWeather.temperature <= snowTemperatureThreshold && 
            (currentWeather.condition == "Snow" || 
             (currentWeather.precipitation >= precipitationThreshold && currentWeather.temperature <= snowTemperatureThreshold)))
        {
            targetVFX = snowVFX;
            
            if (debugMode)
                Debug.Log("[WeatherVFX] Activating snow VFX");
        }
        // Rain/drizzle conditions
        else if (currentWeather.condition == "Rain" || 
                 currentWeather.condition == "Drizzle" ||
                 currentWeather.precipitation >= precipitationThreshold)
        {
            targetVFX = rainVFX;
            
            if (debugMode)
                Debug.Log("[WeatherVFX] Activating rain VFX");
        }
        // Dust/mist conditions
        else if (currentWeather.condition == "Mist" || 
                 currentWeather.condition == "Fog" ||
                 currentWeather.condition == "Haze" ||
                 currentWeather.condition == "Dust" ||
                 currentWeather.condition == "Sand")
        {
            targetVFX = dustVFX;
            
            if (debugMode)
                Debug.Log("[WeatherVFX] Activating dust/mist VFX");
        }
        // Clear weather (optional ambient particles)
        else if (currentWeather.condition == "Clear" && clearVFX != null)
        {
            targetVFX = clearVFX;
            
            if (debugMode)
                Debug.Log("[WeatherVFX] Activating clear weather VFX");
        }
        
        // Switch VFX if needed
        if (targetVFX != activeVFX)
        {
            // Deactivate old VFX
            if (activeVFX != null)
            {
                activeVFX.SetActive(false);
            }
            
            // Activate new VFX
            if (targetVFX != null)
            {
                if (activeVFX == null)
                {
                    activeVFX = Instantiate(targetVFX, transform);
                }
                else
                {
                    // Swap to different VFX
                    Destroy(activeVFX);
                    activeVFX = Instantiate(targetVFX, transform);
                }
                
                // Position above player
                if (player != null)
                {
                    Vector3 vfxPos = player.position;
                    vfxPos.y += vfxHeightOffset;
                    activeVFX.transform.position = vfxPos;
                }
                
                activeVFX.SetActive(true);
                
                if (debugMode)
                    Debug.Log($"[WeatherVFX] VFX changed to: {targetVFX.name}");
            }
        }
    }
    
    // OpenWeatherMap API response structure
    [Serializable]
    private class OpenWeatherResponse
    {
        public Weather[] weather;
        public Main main;
        public Wind wind;
        public Rain rain;
        public Snow snow;
    }
    
    [Serializable]
    private class Weather
    {
        public string main;
        public string description;
    }
    
    [Serializable]
    private class Main
    {
        public float temp;
        public float humidity;
    }
    
    [Serializable]
    private class Wind
    {
        public float speed;
    }
    
    [Serializable]
    private class Rain
    {
        [SerializeField] private float _1h;
        public float oneHour => _1h;
    }
    
    [Serializable]
    private class Snow
    {
        [SerializeField] private float _1h;
        public float oneHour => _1h;
    }
    
    /// <summary>
    /// Get current weather data (for UI display, etc.)
    /// </summary>
    public WeatherData GetCurrentWeather()
    {
        return currentWeather;
    }
    
    /// <summary>
    /// Manually trigger weather update
    /// </summary>
    public void RefreshWeather()
    {
        FetchWeatherData();
    }
    
    void OnDestroy()
    {
        if (activeVFX != null)
        {
            Destroy(activeVFX);
        }
    }
}
