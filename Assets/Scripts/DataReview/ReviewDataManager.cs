using UnityEngine;
using System;

/// <summary>
/// Currently acts as single source of truth/and manager of the higher level stuff when it comes to data preview
/// </summary>
public class ReviewDataManager : MonoBehaviour
{
    private string selectedDataPath; // path to the selected packaged data to review
    private string selectedPackageName; // extracted package name
    const int maxLabelCount = 20; // hard limit to different amount of labels we can have in the data preview

    public static event Action<string> OnDataPathBroadcast;
    public static event Action OnDataLoaded; // Event when data is loaded and ready for display

    private LabelFilterData[] currentFilterSettings;
    private DataLoader dataLoader;
    private ConfigData loadedConfig;

    private void Awake()
    {
        // Get or add DataLoader component
        dataLoader = GetComponent<DataLoader>();
        if (dataLoader == null)
        {
            dataLoader = gameObject.AddComponent<DataLoader>();
        }
    }

    private void Start()
    {
        // Load the annotation config to know how many labels we have
        loadedConfig = ConfigLoader.LoadDefaultAnnotationConfig();

        if (loadedConfig != null && loadedConfig.labels != null)
        {
            // Initialize filter settings array with default values (all included)
            currentFilterSettings = new LabelFilterData[loadedConfig.labels.Count];
            for (int i = 0; i < currentFilterSettings.Length; i++)
            {
                currentFilterSettings[i] = new LabelFilterData(true, 0f, 0, int.MaxValue);
            }

            Debug.Log($"[ReviewDataManager] Initialized with {currentFilterSettings.Length} label filters");
        }
    }

    private void OnEnable()
    {
        OnDataPathBroadcast += UpdateSelectedDataPath;
        DataFilterManager.OnLabelFilterUpdated += UpdateFilterSetting;
    }

    private void OnDisable()
    {
        OnDataPathBroadcast -= UpdateSelectedDataPath;
        DataFilterManager.OnLabelFilterUpdated -= UpdateFilterSetting;
    }

    private void UpdateSelectedDataPath(string newPath)
    {
        selectedDataPath = newPath;

        // Extract package name from path
        // Path format: .../Output/packagedData/PackageName
        selectedPackageName = System.IO.Path.GetFileName(newPath);

        Debug.Log($"[ReviewDataManager] Selected data path updated to: {selectedDataPath}");
        Debug.Log($"[ReviewDataManager] Package name: {selectedPackageName}");

        // Automatically load the data
        LoadSelectedPackageData();
    }

    public static void BroadcastDataPath(string path)
    {
        OnDataPathBroadcast?.Invoke(path);
    }

    private void UpdateFilterSetting(int labelIndex, LabelFilterData newData)
    {
        if (currentFilterSettings == null || labelIndex < 0 || labelIndex >= currentFilterSettings.Length)
        {
            Debug.LogWarning($"[ReviewDataManager] Invalid label index {labelIndex} for filter update");
            return;
        }

        currentFilterSettings[labelIndex] = newData;
        Debug.Log($"[ReviewDataManager] Filter for label {labelIndex} updated. Included={newData.bIsIncludedInData}, Accuracy={newData.percentageAccuracyFilter}, Min={newData.minAmountOfObjects}, Max={newData.maxAmountOfObjects}");

        // Re-apply filters with updated settings
        if (dataLoader != null && dataLoader.GetLoadedImageData().Count > 0)
        {
            RefreshFilteredData();
        }
    }

    /// <summary>
    /// Loads the packaged data for the currently selected package
    /// </summary>
    private void LoadSelectedPackageData()
    {
        if (string.IsNullOrEmpty(selectedPackageName))
        {
            Debug.LogError("[ReviewDataManager] No package name set. Cannot load data.");
            return;
        }

        if (dataLoader == null)
        {
            Debug.LogError("[ReviewDataManager] DataLoader is null!");
            return;
        }

        Debug.Log($"[ReviewDataManager] Loading package data: {selectedPackageName}");

        bool success = dataLoader.LoadPackageData(selectedPackageName);

        if (success)
        {
            Debug.Log($"[ReviewDataManager] Successfully loaded package data. Now applying filters...");
            RefreshFilteredData();
        }
        else
        {
            Debug.LogError($"[ReviewDataManager] Failed to load package data for: {selectedPackageName}");
        }
    }

    /// <summary>
    /// Re-applies filters to the loaded data and notifies UI to update
    /// </summary>
    public void RefreshFilteredData()
    {
        if (dataLoader == null || currentFilterSettings == null)
        {
            Debug.LogWarning("[ReviewDataManager] Cannot refresh filtered data - components not initialized");
            return;
        }

        Debug.Log($"[ReviewDataManager] Applying filters to loaded data...");
        dataLoader.ApplyFilters(currentFilterSettings, maxLabelCount);

        var filteredData = dataLoader.GetFilteredDisplayData();
        Debug.Log($"[ReviewDataManager] Filtered data ready: {filteredData.Count} images");

        // Broadcast that data is ready for display
        OnDataLoaded?.Invoke();
    }

    /// <summary>
    /// Gets the filtered display data (for UI managers to consume)
    /// </summary>
    public System.Collections.Generic.List<ImageDisplayData> GetFilteredDisplayData()
    {
        return dataLoader?.GetFilteredDisplayData();
    }

    /// <summary>
    /// Gets the current filter settings
    /// </summary>
    public LabelFilterData[] GetCurrentFilterSettings()
    {
        return currentFilterSettings;
    }

    /// <summary>
    /// Gets the loaded config data
    /// </summary>
    public ConfigData GetLoadedConfig()
    {
        return loadedConfig;
    }

    /// <summary>
    /// Public method to manually trigger data loading (useful for UI buttons)
    /// </summary>
    public void ManualLoadData()
    {
        if (!string.IsNullOrEmpty(selectedPackageName))
        {
            LoadSelectedPackageData();
        }
        else
        {
            Debug.LogWarning("[ReviewDataManager] No package selected to load");
        }
    }
}