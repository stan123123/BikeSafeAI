using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System;

// Event class for broadcasting filter changes
[System.Serializable]
public class LabelFilterChangeEvent : UnityEvent<int, LabelFilterData> { }

[System.Serializable]
public struct LabelFilterData
{
    public bool bIsIncludedInData;
    public float percentageAccuracyFilter;
    public int minAmountOfObjects;
    public int maxAmountOfObjects;

    public LabelFilterData(bool included, float accuracy, int minAmount, int maxAmount)
    {
        bIsIncludedInData = included;
        percentageAccuracyFilter = accuracy;
        minAmountOfObjects = minAmount;
        maxAmountOfObjects = maxAmount;
    }
}

public class DataFilterManager : MonoBehaviour
{
    [SerializeField] private GameObject LabelFiltersParent;
    [SerializeField] private GameObject labelFilterPrefab;

    // Dynamic list of label filter data, initialized empty
    private List<LabelFilterData> labelFilterStats = new List<LabelFilterData>();

    // Reference to loaded config data
    private ConfigData loadedConfig;

    // List of spawned UI elements for tracking
    private List<LabelFilterUI> spawnedUIElements = new List<LabelFilterUI>();

    public static event Action<int, LabelFilterData> OnLabelFilterUpdated;

    private void Start()
    {
        InitializeFromConfig();
    }

    private void OnEnable()
    {
        LabelFilterUI.OnFilterChanged.AddListener(OnLabelFilterChanged);
    }

    private void OnDisable()
    {
        LabelFilterUI.OnFilterChanged.RemoveListener(OnLabelFilterChanged);
    }

    private void OnLabelFilterChanged(int labelIndex, LabelFilterData newData)
    {
        SetLabelFilterData(labelIndex, newData);
        Debug.Log($"Filter updated for label {labelIndex}: Included={newData.bIsIncludedInData}, Accuracy={newData.percentageAccuracyFilter}");

        // Broadcast to any listeners (e.g., ReviewDataManager)
        OnLabelFilterUpdated?.Invoke(labelIndex, newData);
    }

    /// <summary>
    /// Loads the annotation config and initializes all label filters
    /// </summary>
    public void InitializeFromConfig()
    {
        // Load the config from the default annotation config file
        loadedConfig = ConfigLoader.LoadDefaultAnnotationConfig();

        if (loadedConfig == null || loadedConfig.labels == null)
        {
            Debug.LogError("Failed to load annotation config! Cannot initialize label filters.");
            return;
        }

        int labelCount = loadedConfig.labels.Count;
        Debug.Log($"Initializing DataFilterManager with {labelCount} labels from config");

        // Clear existing children from parent
        ClearLabelFilterUI();

        // Initialize the label filter stats array
        InitializeLabelFilters(labelCount);

        // Spawn UI elements for each label
        SpawnLabelFilterUI();

        Debug.Log($"DataFilterManager initialization complete. Created {spawnedUIElements.Count} label filter UI elements.");
    }

    /// <summary>
    /// Clears all existing label filter UI elements from the parent
    /// </summary>
    private void ClearLabelFilterUI()
    {
        if (LabelFiltersParent == null)
        {
            Debug.LogWarning("LabelFiltersParent is not assigned!");
            return;
        }

        // Destroy all existing children
        for (int i = LabelFiltersParent.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = LabelFiltersParent.transform.GetChild(i);

#if UNITY_EDITOR
            // Use DestroyImmediate in editor for immediate cleanup
            if (!Application.isPlaying)
            {
                DestroyImmediate(child.gameObject);
            }
            else
            {
                Destroy(child.gameObject);
            }
#else
            Destroy(child.gameObject);
#endif
        }

        spawnedUIElements.Clear();
        Debug.Log("Cleared all existing label filter UI elements");
    }

    /// <summary>
    /// Initializes the label filter stats array with default values
    /// </summary>
    public void InitializeLabelFilters(int numberOfLabels)
    {
        labelFilterStats.Clear();

        for (int i = 0; i < numberOfLabels; i++)
        {
            // Default: included, 100% accuracy, 0 min, unlimited max
            labelFilterStats.Add(new LabelFilterData(true, 1f, 0, int.MaxValue));
        }

        Debug.Log($"Initialized {numberOfLabels} label filter data entries");
    }

    /// <summary>
    /// Spawns UI elements for each label in the config
    /// </summary>
    private void SpawnLabelFilterUI()
    {
        if (labelFilterPrefab == null)
        {
            Debug.LogError("LabelFilterUI prefab is not assigned!");
            return;
        }

        if (LabelFiltersParent == null)
        {
            Debug.LogError("LabelFiltersParent is not assigned!");
            return;
        }

        if (loadedConfig == null || loadedConfig.labels == null)
        {
            Debug.LogError("No config data available to spawn UI elements");
            return;
        }

        // Check if the prefab has the LabelFilterUI component
        LabelFilterUI prefabComponent = labelFilterPrefab.GetComponent<LabelFilterUI>();
        if (prefabComponent == null)
        {
            Debug.LogError("The assigned prefab does not have a LabelFilterUI component attached!");
            return;
        }

        for (int i = 0; i < loadedConfig.labels.Count; i++)
        {
            LabelData labelData = loadedConfig.labels[i];

            // Instantiate the prefab under the parent
            GameObject instantiatedObject = Instantiate(labelFilterPrefab, LabelFiltersParent.transform);

            // Get the LabelFilterUI component from the instantiated object
            LabelFilterUI uiElement = instantiatedObject.GetComponent<LabelFilterUI>();

            // Initialize the UI element with label data and index
            uiElement.InitializeElement(labelData, i);

            // Store reference to the spawned element
            spawnedUIElements.Add(uiElement);

            // Optional: Set the name for better hierarchy organization
            instantiatedObject.name = $"LabelFilter_{i}_{labelData.name}";
        }

        Debug.Log($"Spawned {spawnedUIElements.Count} label filter UI elements");
    }

    /// <summary>
    /// Gets the filter data for a specific label index
    /// </summary>
    public LabelFilterData GetLabelFilterData(int labelIndex)
    {
        if (labelIndex >= 0 && labelIndex < labelFilterStats.Count)
        {
            return labelFilterStats[labelIndex];
        }

        Debug.LogWarning($"Invalid label index: {labelIndex}");
        return new LabelFilterData(true, 1f, 0, int.MaxValue);
    }

    /// <summary>
    /// Updates the filter data for a specific label index
    /// </summary>
    public void SetLabelFilterData(int labelIndex, LabelFilterData filterData)
    {
        if (labelIndex >= 0 && labelIndex < labelFilterStats.Count)
        {
            labelFilterStats[labelIndex] = filterData;
        }
        else
        {
            Debug.LogWarning($"Invalid label index: {labelIndex}");
        }
    }

    /// <summary>
    /// Gets the loaded config data (useful for other scripts)
    /// </summary>
    public ConfigData GetLoadedConfig()
    {
        return loadedConfig;
    }

    /// <summary>
    /// Gets all spawned UI elements
    /// </summary>
    public List<LabelFilterUI> GetSpawnedUIElements()
    {
        return spawnedUIElements;
    }

    /// <summary>
    /// Public method to manually trigger re-initialization if needed
    /// </summary>
    public void RefreshFilters()
    {
        InitializeFromConfig();
    }
}