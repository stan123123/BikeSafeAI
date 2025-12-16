using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// Responsible for packaging processed image data into a named batch.
/// </summary>
public class ProcessingResultPackager : MonoBehaviour
{
    public string DefaultPackageName = "PackageName";

    private float userInputFrameDeltaSeconds = 1;

    private bool HasSelectedPackageName = false;

    // Event to trigger packaging
    public static event System.Action OnPackageDataRequested;

    private string packageRoot = null;

    private void OnEnable()
    {
        OnPackageDataRequested += PackageCurrentData;
        ImageProcesser.OnProcessingProgress += CheckIfProcessingFinished;
    }

    private void OnDisable()
    {
        OnPackageDataRequested -= PackageCurrentData;
        ImageProcesser.OnProcessingProgress -= CheckIfProcessingFinished;
    }

    private void setUserInputFrameDeltaSeconds(float newValue)
    {
        userInputFrameDeltaSeconds = newValue;
    }

    /// <summary>
    /// Packages the temp images into a new folder under packagedData
    /// </summary>
    private void PackageCurrentData()
    {
        string usedPackageName = ProcessingUserSelectionManager.SelectedVideoName;

        if (string.IsNullOrEmpty(usedPackageName))
        {
            usedPackageName = GetAvailablePackageName(DefaultPackageName);
        }

        packageRoot = PathConfig.CreatePackagedDataFolders(usedPackageName);

        // Copy temp used images
        foreach (string file in Directory.GetFiles(PathConfig.UsedImagesFolder))
        {
            string dest = Path.Combine(PathConfig.GetPackagedUsedImagesFolder(usedPackageName), Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        // Copy all annotated images
        foreach (string file in Directory.GetFiles(PathConfig.AnnotatedImagesFolder))
        {
            string dest = Path.Combine(PathConfig.GetPackagedAnnotatedImagesFolder(usedPackageName), Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        var legend = ConfigLoader.GetLabels(ConfigLoader.LoadDefaultAnnotationConfig());

        if (legend == null || legend.Length == 0)
        {
            Debug.LogError("Failed to load label legend. Cannot analyze images.");
            return;
        }

        // Process only *_panoptic-colored-mask_objects.json files
        string annotatedFolder = PathConfig.GetPackagedAnnotatedImagesFolder(usedPackageName);
        foreach (string objectsJsonPath in Directory.GetFiles(annotatedFolder)
                                                     .Where(f => f.EndsWith("_panoptic-colored-mask_objects.json")))
        {
            // Extract base name from the objects JSON filename
            string fileName = Path.GetFileNameWithoutExtension(objectsJsonPath);
            // Remove the "_panoptic-colored-mask_objects" suffix to get base name
            string baseName = fileName.Replace("_panoptic-colored-mask_objects", "");

            // Verify that the corresponding panoptic masks exist
            string panopticFusedMaskPath = Path.Combine(annotatedFolder, baseName + "_panoptic-fused-colored-mask.png");
            string panopticColoredMaskPath = Path.Combine(annotatedFolder, baseName + "_panoptic-colored-mask.png");

            bool hasFusedMask = File.Exists(panopticFusedMaskPath);
            bool hasColoredMask = File.Exists(panopticColoredMaskPath);

            if (!hasFusedMask && !hasColoredMask)
            {
                Debug.LogWarning($"No panoptic masks found for {baseName}. Skipping analysis.");
                continue;
            }

            // Analyze using the panoptic objects JSON
            var data = AnnotationAnalyzer.AnalyzeImage(legend, objectsJsonPath, baseName);
            AnnotationAnalyzer.SaveToJson(data, usedPackageName);

            Debug.Log($"Analyzed {baseName}: {data.totalInstances} instances across {data.labels.Count} label types");
        }

        Debug.Log($"Packaged data for '{usedPackageName}' successfully!");
    }

    private string GetAvailablePackageName(string baseName)
    {
        // Check if base name is available
        if (ProcessedFolderNameValidator.IsFolderNameAvailable(baseName))
        {
            return baseName;
        }

        // If not, start incrementing
        int counter = 1;
        string candidateName;

        do
        {
            candidateName = $"{baseName}_{counter:D2}"; // D2 formats as 01, 02, 03, etc.
            counter++;
        }
        while (!ProcessedFolderNameValidator.IsFolderNameAvailable(candidateName));

        Debug.Log($"Package name '{baseName}' already exists. Using '{candidateName}' instead.");

        return candidateName;
    }

    public static void RequestPackageData()
    {
        OnPackageDataRequested?.Invoke();
    }

    private void CheckIfProcessingFinished(int currentProgress, int totalToProcess)
    {
        packageRoot = PathConfig.CreatePackagedDataFolders(ProcessingUserSelectionManager.SelectedVideoName);

        if (currentProgress == totalToProcess)
        {
            createProcessedVideoData(true, currentProgress);
        }
        else
        {
            createProcessedVideoData(false, currentProgress);
        }
    }

    private void createProcessedVideoData(bool isDoneProcessing, int totalFrames)
    {
        if (packageRoot == null) 
        {
            Debug.LogError("PackageRoot is Null, cannot save processed folder data.");

            return;
        }

        ProcessedVideoData data = new ProcessedVideoData
        {
            hasGPSData = false,
            totalFrames = totalFrames,
            videoDurationSeconds = 1,
            userInputFrameDeltaSeconds = ProcessingUserSelectionManager.SelectedDeltaSeconds,
            hasFinishedProcessingWithoutError = isDoneProcessing
        };

        // Serialize to JSON
        string json = JsonUtility.ToJson(data, true);

        // Combine path with filename (assuming you have a constant for the name)
        string fullPath = Path.Combine(packageRoot, "ProcessedVideoData.json");

        // Write JSON to file
        File.WriteAllText(fullPath, json);

        Debug.Log($"ProcessedVideoData saved to: {fullPath}");
    }

    /// <summary>
    /// Call this line from anywhere to broadcast the event and trigger packaging:
    /// </summary>
    // ProcessingResultPackager.OnPackageDataRequested?.Invoke();
}