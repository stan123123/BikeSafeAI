using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Manages the UI for data preview panels, spawning one panel per image
/// </summary>
public class DataPreviewUIManager : MonoBehaviour
{

    [SerializeField] private GameObject DataPreviewPanelPrefab;
    [SerializeField] private GameObject DataPreviewPanelParent;
    [SerializeField] private ReviewDataManager reviewDataManager;

    private List<DataPreviewPanel> spawnedPanels = new List<DataPreviewPanel>();

    [SerializeField] private GameObject ObjectNameTextPrefab;
    [SerializeField] private GameObject ObjectNameTextParent; // parent we will spawn the object names under

    private void Start()
    {
        // Find ReviewDataManager if not assigned
        if (reviewDataManager == null)
        {
            reviewDataManager = FindObjectOfType<ReviewDataManager>();
            if (reviewDataManager == null)
            {
                Debug.LogError("[DataPreviewUIManager] ReviewDataManager not found in scene!");
            }
        }
    }

    private void OnEnable()
    {
        ReviewDataManager.OnDataLoaded += RefreshDisplay;

        RefreshDisplay();
    }

    private void OnDisable()
    {
        ReviewDataManager.OnDataLoaded -= RefreshDisplay;
    }

    /// <summary>
    /// Refreshes the entire display by clearing and respawning all panels
    /// </summary>
    public void RefreshDisplay()
    {
        if (reviewDataManager == null)
        {
            Debug.LogError("[DataPreviewUIManager] ReviewDataManager reference is null!");
            return;
        }

        // Clear existing panels
        ClearAllPanels();

        // Get filtered data from the ReviewDataManager
        List<ImageDisplayData> displayData = reviewDataManager.GetFilteredDisplayData();

        if (displayData == null || displayData.Count == 0)
        {
            Debug.LogWarning("[DataPreviewUIManager] No display data available to show");
            return;
        }

        Debug.Log($"[DataPreviewUIManager] Spawning {displayData.Count} preview panels");

        // Spawn a panel for each image
        foreach (ImageDisplayData imageData in displayData)
        {
            SpawnPreviewPanel(imageData);
        }

        SpawnEnabledLabelNames();

        Debug.Log($"[DataPreviewUIManager] Successfully spawned {spawnedPanels.Count} preview panels");
    }

    /// <summary>
    /// Spawns a single preview panel for an image
    /// </summary>
    private void SpawnPreviewPanel(ImageDisplayData imageData)
    {
        if (DataPreviewPanelPrefab == null)
        {
            Debug.LogError("[DataPreviewUIManager] DataPreviewPanelPrefab is not assigned!");
            return;
        }

        if (DataPreviewPanelParent == null)
        {
            Debug.LogError("[DataPreviewUIManager] DataPreviewPanelParent is not assigned!");
            return;
        }

        // Instantiate the panel
        GameObject panelObj = Instantiate(DataPreviewPanelPrefab, DataPreviewPanelParent.transform);
        panelObj.name = $"Panel_{imageData.imageName}";

        // Get the DataPreviewPanel component
        DataPreviewPanel panel = panelObj.GetComponent<DataPreviewPanel>();

        if (panel == null)
        {
            Debug.LogError($"[DataPreviewUIManager] Spawned panel doesn't have DataPreviewPanel component: {panelObj.name}");
            Destroy(panelObj);
            return;
        }

        // Normalize the counts to 0-20 range for color intensity
        int[] normalizedValues = NormalizeToDisplayRange(imageData.labelCounts);

        // Initialize the panel with the normalized values
        panel.Initialize(normalizedValues);

        // Store reference
        spawnedPanels.Add(panel);
    }

    /// <summary>
    /// Spawns text labels for the top enabled labels from the config (instead of image names)
    /// </summary>
    private void SpawnEnabledLabelNames()
    {
        if (reviewDataManager == null)
            return;

        var filterSettings = reviewDataManager.GetCurrentFilterSettings();
        var config = reviewDataManager.GetLoadedConfig();

        if (filterSettings == null || config == null || config.labels == null)
            return;

        int labelsToShow = Mathf.Min(filterSettings.Length, 20); // max 20

        for (int i = 0; i < labelsToShow; i++)
        {
            if (filterSettings[i].bIsIncludedInData)
            {
                string labelName = config.labels[i].readable;
                SpawnObjectNameText(labelName);
            }
        }
    }

    /// <summary>
    /// Spawns a text label for an object name
    /// </summary>
    private void SpawnObjectNameText(string objectName)
    {
        if (ObjectNameTextPrefab == null)
        {
            Debug.LogError("[DataPreviewUIManager] ObjectNameTextPrefab is not assigned!");
            return;
        }

        if (ObjectNameTextParent == null)
        {
            Debug.LogError("[DataPreviewUIManager] ObjectNameTextParent is not assigned!");
            return;
        }

        // Instantiate the text prefab
        GameObject textObj = Instantiate(ObjectNameTextPrefab, ObjectNameTextParent.transform);
        textObj.name = $"Text_{objectName}";

        // Get the TextMeshProUGUI component and set the text
        TextMeshProUGUI textComponent = textObj.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = objectName;
        }
        else
        {
            Debug.LogWarning($"[DataPreviewUIManager] TextMeshProUGUI component not found on {textObj.name}");
        }
    }

    /// <summary>
    /// Normalizes instance counts to the 0-20 range for display
    /// Uses adaptive scaling based on max value in the data
    /// </summary>
    private int[] NormalizeToDisplayRange(int[] rawCounts)
    {
        if (rawCounts == null || rawCounts.Length == 0)
            return new int[0];

        int[] normalized = new int[rawCounts.Length];

        // Find the maximum count across all labels
        int maxCount = 0;
        foreach (int count in rawCounts)
        {
            if (count > maxCount)
                maxCount = count;
        }

        // If all counts are 0, return all zeros
        if (maxCount == 0)
        {
            return normalized; // Already initialized to zeros
        }

        // Normalize to 0-20 range
        for (int i = 0; i < rawCounts.Length; i++)
        {
            if (maxCount <= 20)
            {
                // If max is already within range, use direct values
                normalized[i] = rawCounts[i];
            }
            else
            {
                // Scale proportionally to fit 0-20 range
                normalized[i] = Mathf.RoundToInt((rawCounts[i] / (float)maxCount) * 20f);
            }
        }

        return normalized;
    }

    /// <summary>
    /// Clears all spawned preview panels and object name texts
    /// </summary>
    private void ClearAllPanels()
    {
        foreach (DataPreviewPanel panel in spawnedPanels)
        {
            if (panel != null && panel.gameObject != null)
            {
                Destroy(panel.gameObject);
            }
        }

        spawnedPanels.Clear();

        // Also clear any orphaned children in the panel parent
        if (DataPreviewPanelParent != null)
        {
            for (int i = DataPreviewPanelParent.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = DataPreviewPanelParent.transform.GetChild(i);
                Destroy(child.gameObject);
            }
        }

        // Clear all object name text children
        if (ObjectNameTextParent != null)
        {
            for (int i = ObjectNameTextParent.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = ObjectNameTextParent.transform.GetChild(i);
                Destroy(child.gameObject);
            }
        }

        Debug.Log("[DataPreviewUIManager] Cleared all preview panels and object name texts");
    }

    /// <summary>
    /// Public method to manually trigger a refresh (useful for UI buttons)
    /// </summary>
    public void ManualRefresh()
    {
        RefreshDisplay();
    }

    /// <summary>
    /// Gets the count of currently displayed panels
    /// </summary>
    public int GetPanelCount()
    {
        return spawnedPanels.Count;
    }
}