using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Handles loading and filtering of packaged annotation data from disk
/// </summary>
public class DataLoader : MonoBehaviour
{
    // All loaded image data from the package
    private List<ImageAnnotationData> loadedImageData = new List<ImageAnnotationData>();

    // Filtered data ready for display
    private List<ImageDisplayData> filteredDisplayData = new List<ImageDisplayData>();

    // Current package name being loaded
    private string currentPackageName;

    /// <summary>
    /// Loads all JSON files from the imageStatsJson folder of a packaged data directory
    /// </summary>
    /// <param name="packageName">Name of the package to load</param>
    /// <returns>True if loading was successful</returns>
    public bool LoadPackageData(string packageName)
    {
        currentPackageName = packageName;
        loadedImageData.Clear();

        string imageStatsFolder = PathConfig.GetPackagedImageStatsJsonFolder(packageName);

        if (!Directory.Exists(imageStatsFolder))
        {
            Debug.LogError($"[DataLoader] Image stats folder not found: {imageStatsFolder}");
            return false;
        }

        // Get all JSON files in the folder
        string[] jsonFiles = Directory.GetFiles(imageStatsFolder, "*_analysis.json");

        if (jsonFiles.Length == 0)
        {
            Debug.LogWarning($"[DataLoader] No analysis JSON files found in: {imageStatsFolder}");
            return false;
        }

        Debug.Log($"[DataLoader] Found {jsonFiles.Length} JSON files to load");

        // Load each JSON file
        int successCount = 0;
        foreach (string jsonPath in jsonFiles)
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonPath);
                ImageAnnotationData imageData = JsonUtility.FromJson<ImageAnnotationData>(jsonContent);

                if (imageData != null)
                {
                    loadedImageData.Add(imageData);
                    successCount++;
                }
                else
                {
                    Debug.LogWarning($"[DataLoader] Failed to deserialize JSON: {jsonPath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DataLoader] Error loading JSON file {jsonPath}: {e.Message}");
            }
        }

        Debug.Log($"[DataLoader] Successfully loaded {successCount}/{jsonFiles.Length} image annotation files");
        return successCount > 0;
    }

    /// <summary>
    /// Filters the loaded data based on current filter settings
    /// </summary>
    /// <param name="filterSettings">Array of filter settings per label (index = labelId)</param>
    /// <param name="maxLabelCount">Maximum number of labels to include in display</param>
    public void ApplyFilters(LabelFilterData[] filterSettings, int maxLabelCount = 20)
    {
        filteredDisplayData.Clear();

        if (loadedImageData.Count == 0)
        {
            Debug.LogWarning("[DataLoader] No data loaded to filter");
            return;
        }

        if (filterSettings == null || filterSettings.Length == 0)
        {
            Debug.LogWarning("[DataLoader] No filter settings provided");
            return;
        }

        // Determine which labels are included (up to maxLabelCount)
        List<int> includedLabelIndices = new List<int>();
        for (int i = 0; i < Mathf.Min(filterSettings.Length, maxLabelCount); i++)
        {
            if (filterSettings[i].bIsIncludedInData)
            {
                includedLabelIndices.Add(i);
            }
        }

        if (includedLabelIndices.Count == 0)
        {
            Debug.LogWarning("[DataLoader] No labels are included in the filter");
            return;
        }

        Debug.Log($"[DataLoader] Filtering data for {includedLabelIndices.Count} included labels: {string.Join(", ", includedLabelIndices)}");

        // Process each image
        int imagesPassedFilter = 0;
        foreach (ImageAnnotationData imageData in loadedImageData)
        {
            ImageDisplayData displayData = ProcessImageForDisplay(imageData, filterSettings, includedLabelIndices);

            // Only add if the image passes all filters
            if (displayData != null)
            {
                filteredDisplayData.Add(displayData);
                imagesPassedFilter++;
            }
        }

        Debug.Log($"[DataLoader] Filtered data: {imagesPassedFilter}/{loadedImageData.Count} images passed filters");
    }

    /// <summary>
    /// Processes a single image to create display data based on filters
    /// </summary>
    private ImageDisplayData ProcessImageForDisplay(ImageAnnotationData imageData, LabelFilterData[] filterSettings, List<int> includedLabelIndices)
    {
        ImageDisplayData displayData = new ImageDisplayData
        {
            imageName = imageData.imageName,
            labelCounts = new int[includedLabelIndices.Count]
        };

        bool imagePassesFilter = true;

        // For each included label, count instances that pass the filter
        for (int displayIndex = 0; displayIndex < includedLabelIndices.Count; displayIndex++)
        {
            int labelId = includedLabelIndices[displayIndex];
            LabelFilterData filter = filterSettings[labelId];

            // Find the label stats for this labelId in the image data
            // NOTE: imageData.labels is a sparse array - it only contains labels that were detected
            LabelStats labelStats = null;
            if (imageData.labels != null)
            {
                labelStats = imageData.labels.Find(ls => ls.labelId == labelId);
            }

            int validInstanceCount = 0;

            if (labelStats != null && labelStats.instances != null)
            {
                // Count instances that meet the accuracy threshold
                foreach (InstanceInfo instance in labelStats.instances)
                {
                    if (instance.score >= filter.percentageAccuracyFilter)
                    {
                        validInstanceCount++;
                    }
                }
            }

            // Check if this label's instance count is within the min/max range
            if (validInstanceCount < filter.minAmountOfObjects || validInstanceCount > filter.maxAmountOfObjects)
            {
                // This image doesn't meet the filter criteria for this label
                // Mark it as failing the filter
                imagePassesFilter = false;
                break; // Exit early since this image fails the filter
            }

            // Store the count for this label (will be 0 if label wasn't detected)
            displayData.labelCounts[displayIndex] = validInstanceCount;
        }

        // Return null if image doesn't pass the filter (will be excluded from display)
        if (!imagePassesFilter)
        {
            return null;
        }

        return displayData;
    }

    /// <summary>
    /// Gets the filtered display data ready for UI rendering
    /// </summary>
    public List<ImageDisplayData> GetFilteredDisplayData()
    {
        return filteredDisplayData;
    }

    /// <summary>
    /// Gets all loaded raw image data
    /// </summary>
    public List<ImageAnnotationData> GetLoadedImageData()
    {
        return loadedImageData;
    }

    /// <summary>
    /// Gets the current package name
    /// </summary>
    public string GetCurrentPackageName()
    {
        return currentPackageName;
    }

    /// <summary>
    /// Gets statistics about the loaded data
    /// </summary>
    public string GetLoadedDataStats()
    {
        if (loadedImageData.Count == 0)
            return "No data loaded";

        int totalInstances = 0;
        Dictionary<int, int> labelCounts = new Dictionary<int, int>();

        foreach (var imageData in loadedImageData)
        {
            totalInstances += imageData.totalInstances;

            if (imageData.labels != null)
            {
                foreach (var labelStats in imageData.labels)
                {
                    if (!labelCounts.ContainsKey(labelStats.labelId))
                        labelCounts[labelStats.labelId] = 0;

                    labelCounts[labelStats.labelId] += labelStats.instanceCount;
                }
            }
        }

        return $"Loaded {loadedImageData.Count} images with {totalInstances} total instances across {labelCounts.Count} different labels";
    }
}

/// <summary>
/// Represents display data for a single image after filtering
/// </summary>
[System.Serializable]
public class ImageDisplayData
{
    public string imageName;
    public int[] labelCounts; // Array indexed by display position (only included labels)
    // Index in this array corresponds to the position in the includedLabelIndices list
    // NOT the original labelId from the config
}