using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// Responsible for packaging processed image data into a named batch.
/// </summary>
public class ProcessingResultPackager : MonoBehaviour
{
    public string PackageName = "PackageName";  // Name selected by user for this batch

    // Event to trigger packaging (no arguments)
    public static event System.Action OnPackageDataRequested;

    private void OnEnable()
    {
        OnPackageDataRequested += PackageCurrentData;
    }

    private void OnDisable()
    {
        OnPackageDataRequested -= PackageCurrentData;
    }

    /// <summary>
    /// Packages the temp images into a new folder under packagedData
    /// </summary>
    private void PackageCurrentData()
    {
        if (string.IsNullOrEmpty(PackageName))
            PackageName = "UnnamedPackage";

        string packageRoot = PathConfig.CreatePackagedDataFolders(PackageName);

        // Copy temp used images
        foreach (string file in Directory.GetFiles(PathConfig.UsedImagesFolder))
        {
            string dest = Path.Combine(PathConfig.GetPackagedUsedImagesFolder(PackageName), Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        // Copy all annotated images
        foreach (string file in Directory.GetFiles(PathConfig.AnnotatedImagesFolder))
        {
            string dest = Path.Combine(PathConfig.GetPackagedAnnotatedImagesFolder(PackageName), Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        var legend = ConfigLoader.GetLabels(ConfigLoader.LoadDefaultAnnotationConfig());

        if (legend == null || legend.Length == 0)
        {
            Debug.LogError("Failed to load label legend. Cannot analyze images.");
            return;
        }

        // Process only *_panoptic-colored-mask_objects.json files
        string annotatedFolder = PathConfig.GetPackagedAnnotatedImagesFolder(PackageName);
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
            AnnotationAnalyzer.SaveToJson(data, PackageName);

            Debug.Log($"Analyzed {baseName}: {data.totalInstances} instances across {data.labels.Count} label types");
        }

        Debug.Log($"Packaged data for '{PackageName}' successfully!");
    }

    public static void RequestPackageData()
    {
        OnPackageDataRequested?.Invoke();
    }

    /// <summary>
    /// Call this line from anywhere to broadcast the event and trigger packaging:
    /// </summary>
    // ProcessingResultPackager.OnPackageDataRequested?.Invoke();
}