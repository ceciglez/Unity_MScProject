using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Advanced filtering and search features for iNaturalist observations
/// </summary>
public class INaturalistFilterManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private INaturalistMapController mapController;
    
    [Header("Filter Settings")]
    [SerializeField] private bool filterByTaxon = false;
    [SerializeField] private string[] iconicTaxa = new string[] { }; // e.g., "Aves", "Plantae", "Insecta"
    [SerializeField] private bool filterByQuality = false;
    [SerializeField] private string qualityGrade = "research"; // research, needs_id, casual
    [SerializeField] private bool filterByDate = false;
    [SerializeField] private string dateFrom = ""; // YYYY-MM-DD
    [SerializeField] private string dateTo = ""; // YYYY-MM-DD
    [SerializeField] private bool filterByUser = false;
    [SerializeField] private string userId = "";
    
    [Header("Search")]
    [SerializeField] private string searchTerm = "";
    [SerializeField] private bool searchInProgress = false;
    
    private const string INATURALIST_API_URL = "https://api.inaturalist.org/v1/observations";
    private const string TAXA_SEARCH_URL = "https://api.inaturalist.org/v1/taxa";
    
    /// <summary>
    /// Build a filtered API URL based on current settings
    /// </summary>
    public string BuildFilteredAPIUrl(float swlat, float swlng, float nelat, float nelng, int perPage = 100)
    {
        string url = $"{INATURALIST_API_URL}?" +
                     $"swlng={swlng}&swlat={swlat}&nelng={nelng}&nelat={nelat}" +
                     $"&per_page={perPage}&order=desc&order_by=created_at" +
                     $"&photos=true&captive=false";
        
        // Quality grade filter
        if (filterByQuality && !string.IsNullOrEmpty(qualityGrade))
        {
            url += $"&quality_grade={qualityGrade}";
        }
        
        // Iconic taxa filter
        if (filterByTaxon && iconicTaxa != null && iconicTaxa.Length > 0)
        {
            foreach (string taxon in iconicTaxa)
            {
                url += $"&iconic_taxa={taxon}";
            }
        }
        
        // Date range filter
        if (filterByDate)
        {
            if (!string.IsNullOrEmpty(dateFrom))
            {
                url += $"&d1={dateFrom}";
            }
            if (!string.IsNullOrEmpty(dateTo))
            {
                url += $"&d2={dateTo}";
            }
        }
        
        // User filter
        if (filterByUser && !string.IsNullOrEmpty(userId))
        {
            url += $"&user_id={userId}";
        }
        
        return url;
    }
    
    /// <summary>
    /// Search for taxa by name
    /// </summary>
    public IEnumerator SearchTaxa(string query, System.Action<TaxaSearchResult[]> callback)
    {
        if (string.IsNullOrEmpty(query))
        {
            callback?.Invoke(new TaxaSearchResult[0]);
            yield break;
        }
        
        searchInProgress = true;
        
        string url = $"{TAXA_SEARCH_URL}?q={UnityWebRequest.EscapeURL(query)}&per_page=20";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    TaxaSearchResponse response = JsonUtility.FromJson<TaxaSearchResponse>(request.downloadHandler.text);
                    callback?.Invoke(response.results);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing taxa search: {e.Message}");
                    callback?.Invoke(new TaxaSearchResult[0]);
                }
            }
            else
            {
                Debug.LogError($"Taxa search failed: {request.error}");
                callback?.Invoke(new TaxaSearchResult[0]);
            }
        }
        
        searchInProgress = false;
    }
    
    /// <summary>
    /// Apply current filters and reload observations
    /// </summary>
    public void ApplyFilters()
    {
        if (mapController != null)
        {
            mapController.ReloadData();
        }
    }
    
    /// <summary>
    /// Clear all filters
    /// </summary>
    public void ClearFilters()
    {
        filterByTaxon = false;
        iconicTaxa = new string[0];
        filterByQuality = false;
        filterByDate = false;
        dateFrom = "";
        dateTo = "";
        filterByUser = false;
        userId = "";
        
        ApplyFilters();
    }
    
    /// <summary>
    /// Set taxon filter
    /// </summary>
    public void SetTaxonFilter(string[] taxa)
    {
        iconicTaxa = taxa;
        filterByTaxon = taxa != null && taxa.Length > 0;
    }
    
    /// <summary>
    /// Set quality grade filter
    /// </summary>
    public void SetQualityFilter(string quality)
    {
        qualityGrade = quality;
        filterByQuality = !string.IsNullOrEmpty(quality);
    }
    
    /// <summary>
    /// Set date range filter
    /// </summary>
    public void SetDateRange(string from, string to)
    {
        dateFrom = from;
        dateTo = to;
        filterByDate = !string.IsNullOrEmpty(from) || !string.IsNullOrEmpty(to);
    }
    
    /// <summary>
    /// Set user filter
    /// </summary>
    public void SetUserFilter(string user)
    {
        userId = user;
        filterByUser = !string.IsNullOrEmpty(user);
    }
    
    /// <summary>
    /// Get current filter summary
    /// </summary>
    public string GetFilterSummary()
    {
        List<string> filters = new List<string>();
        
        if (filterByTaxon && iconicTaxa != null && iconicTaxa.Length > 0)
        {
            filters.Add($"Taxa: {string.Join(", ", iconicTaxa)}");
        }
        
        if (filterByQuality)
        {
            filters.Add($"Quality: {qualityGrade}");
        }
        
        if (filterByDate)
        {
            if (!string.IsNullOrEmpty(dateFrom) && !string.IsNullOrEmpty(dateTo))
            {
                filters.Add($"Date: {dateFrom} to {dateTo}");
            }
            else if (!string.IsNullOrEmpty(dateFrom))
            {
                filters.Add($"Date: from {dateFrom}");
            }
            else if (!string.IsNullOrEmpty(dateTo))
            {
                filters.Add($"Date: until {dateTo}");
            }
        }
        
        if (filterByUser)
        {
            filters.Add($"User: {userId}");
        }
        
        return filters.Count > 0 ? string.Join(", ", filters) : "No filters active";
    }
}

// Additional data structures for taxa search
[Serializable]
public class TaxaSearchResponse
{
    public int total_results;
    public TaxaSearchResult[] results;
}

[Serializable]
public class TaxaSearchResult
{
    public int id;
    public string name;
    public string preferred_common_name;
    public string rank;
    public string iconic_taxon_name;
}

// Predefined iconic taxa for easy reference
public static class IconicTaxa
{
    public const string AMPHIBIANS = "Amphibia";
    public const string ANIMALS = "Animalia";
    public const string ARACHNIDS = "Arachnida";
    public const string BIRDS = "Aves";
    public const string CHROMISTA = "Chromista";
    public const string FISH = "Actinopterygii";
    public const string FUNGI = "Fungi";
    public const string INSECTS = "Insecta";
    public const string MAMMALS = "Mammalia";
    public const string MOLLUSKS = "Mollusca";
    public const string PLANTS = "Plantae";
    public const string PROTOZOA = "Protozoa";
    public const string REPTILES = "Reptilia";
    
    public static string[] GetAllTaxa()
    {
        return new string[]
        {
            AMPHIBIANS, ANIMALS, ARACHNIDS, BIRDS, CHROMISTA,
            FISH, FUNGI, INSECTS, MAMMALS, MOLLUSKS,
            PLANTS, PROTOZOA, REPTILES
        };
    }
}

// Quality grades for easy reference
public static class QualityGrades
{
    public const string RESEARCH = "research";
    public const string NEEDS_ID = "needs_id";
    public const string CASUAL = "casual";
    
    public static string[] GetAllGrades()
    {
        return new string[] { RESEARCH, NEEDS_ID, CASUAL };
    }
}
