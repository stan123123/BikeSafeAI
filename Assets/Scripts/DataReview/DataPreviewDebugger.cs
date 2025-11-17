using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Debug utilities for inspecting loaded data and filter settings
/// </summary>
public class DataPreviewDebugger : MonoBehaviour
{
    [SerializeField] private ReviewDataManager reviewDataManager;
    [SerializeField] private DataFilterManager dataFilterManager;
    [SerializeField] private DataPreviewUIManager dataPreviewUIManager;

    [Header("Debug Options")]
    [SerializeField] private bool logDetailedStats = true;
    [SerializeField] private bool logFilterSettings = true;
    [SerializeField] private bool logDisplayData = true;

    private void Start()
    {
        if (reviewDataManager == null)
            reviewDataManager = FindObjectOfType<ReviewDataManager>();

        if (dataFilterManager == null)
            dataFilterManager = FindObjectOfType<DataFilterManager>();

        if (dataPreviewUIManager == null)
            dataPreviewUIManager = FindObjectOfType<DataPreviewUIManager>();
    }

    /// <summary>
    /// Logs comprehensive information about the current state
    /// </summary>
    [ContextMenu("Debug: Log All Info")]
    public void LogAllInfo()
    {
        Debug.Log("=== DATA PREVIEW DEBUG INFO ===");

        if (reviewDataManager != null)
        {
            LogLoadedDataInfo();

            if (logFilterSettings)
                LogFilterSettings();

            if (logDisplayData)
                LogDisplayData();
        }
        else
        {
            Debug.LogError("ReviewDataManager not found!");
        }

        if (dataPreviewUIManager != null)
        {
            Debug.Log($"[UI] Currently displaying {dataPreviewUIManager.GetPanelCount()} panels");
        }

        Debug.Log("=== END DEBUG INFO ===");
    }

    /// <summary>
    /// Logs information about loaded data
    /// </summary>
    [ContextMenu("Debug: Log Loaded Data")]
    public void LogLoadedDataInfo()
    {
        var displayData = reviewDataManager?.GetFilteredDisplayData();
        var config = reviewDataManager?.GetLoadedConfig();

        if (config == null)
        {
            Debug.LogWarning("No config loaded");
            return;
        }

        Debug.Log($"[Config] Total labels in config: {config.labels.Count}");

        if (displayData == null || displayData.Count == 0)
        {
            Debug.LogWarning("No display data available");
            return;
        }

        Debug.Log($"[Data] Total images passing filters: {displayData.Count}");

        if (logDetailedStats)
        {
            // Sample first few images
            int sampleCount = Mathf.Min(5, displayData.Count);
            Debug.Log($"[Data] Showing first {sampleCount} images:");

            for (int i = 0; i < sampleCount; i++)
            {
                var img = displayData[i];
                Debug.Log($"  Image '{img.imageName}': {img.labelCounts.Length} labels tracked, " +
                          $"counts = [{string.Join(", ", img.labelCounts)}]");
            }
        }
    }

    /// <summary>
    /// Logs current filter settings
    /// </summary>
    [ContextMenu("Debug: Log Filter Settings")]
    public void LogFilterSettings()
    {
        var filterSettings = reviewDataManager?.GetCurrentFilterSettings();
        var config = reviewDataManager?.GetLoadedConfig();

        if (filterSettings == null || config == null)
        {
            Debug.LogWarning("Filter settings or config not available");
            return;
        }

        Debug.Log($"[Filters] Total filter settings: {filterSettings.Length}");

        List<int> includedLabels = new List<int>();
        for (int i = 0; i < filterSettings.Length; i++)
        {
            if (filterSettings[i].bIsIncludedInData)
            {
                includedLabels.Add(i);
            }
        }

        Debug.Log($"[Filters] Included labels: {includedLabels.Count}/{filterSettings.Length}");

        if (logDetailedStats)
        {
            Debug.Log("[Filters] Included label details:");
            foreach (int labelId in includedLabels)
            {
                var filter = filterSettings[labelId];
                var labelInfo = config.labels[labelId];

                Debug.Log($"  [{labelId}] {labelInfo.name} ({labelInfo.readable}): " +
                          $"Accuracy>={filter.percentageAccuracyFilter:F2}, " +
                          $"Count={filter.minAmountOfObjects}-{filter.maxAmountOfObjects}");
            }
        }
    }

    /// <summary>
    /// Logs detailed display data
    /// </summary>
    [ContextMenu("Debug: Log Display Data")]
    public void LogDisplayData()
    {
        var displayData = reviewDataManager?.GetFilteredDisplayData();
        var filterSettings = reviewDataManager?.GetCurrentFilterSettings();
        var config = reviewDataManager?.GetLoadedConfig();

        if (displayData == null || filterSettings == null || config == null)
        {
            Debug.LogWarning("Display data not available");
            return;
        }

        // Get included label names
        List<string> includedLabelNames = new List<string>();
        for (int i = 0; i < filterSettings.Length; i++)
        {
            if (filterSettings[i].bIsIncludedInData && i < 20)
            {
                includedLabelNames.Add(config.labels[i].readable);
            }
        }

        Debug.Log($"[Display] Label order: {string.Join(", ", includedLabelNames)}");

        // Calculate statistics across all images
        if (displayData.Count > 0)
        {
            int labelCount = displayData[0].labelCounts.Length;
            int[] totals = new int[labelCount];
            int[] maxValues = new int[labelCount];

            foreach (var img in displayData)
            {
                for (int i = 0; i < img.labelCounts.Length && i < labelCount; i++)
                {
                    totals[i] += img.labelCounts[i];
                    maxValues[i] = Mathf.Max(maxValues[i], img.labelCounts[i]);
                }
            }

            Debug.Log("[Display] Aggregate statistics across all displayed images:");
            for (int i = 0; i < labelCount; i++)
            {
                float avg = (float)totals[i] / displayData.Count;
                Debug.Log($"  {includedLabelNames[i]}: Total={totals[i]}, Max={maxValues[i]}, Avg={avg:F2}");
            }
        }
    }

    /// <summary>
    /// Simulates loading a specific package (useful for testing)
    /// </summary>
    [ContextMenu("Debug: Trigger Manual Load")]
    public void TriggerManualLoad()
    {
        if (reviewDataManager != null)
        {
            reviewDataManager.ManualLoadData();
        }
        else
        {
            Debug.LogError("ReviewDataManager not assigned!");
        }
    }

    /// <summary>
    /// Refreshes the display manually
    /// </summary>
    [ContextMenu("Debug: Refresh Display")]
    public void RefreshDisplay()
    {
        if (dataPreviewUIManager != null)
        {
            dataPreviewUIManager.ManualRefresh();
        }
        else
        {
            Debug.LogError("DataPreviewUIManager not assigned!");
        }
    }
}