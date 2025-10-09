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

        // Create the folder structure
        string packageRoot = PathConfig.CreatePackagedDataFolders(PackageName);

        // Copy temp used images
        foreach (string file in Directory.GetFiles(PathConfig.UsedImagesFolder))
        {
            string dest = Path.Combine(PathConfig.GetPackagedUsedImagesFolder(PackageName), Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        // Copy temp annotated images
        foreach (string file in Directory.GetFiles(PathConfig.AnnotatedImagesFolder)
                                 .Where(f => f.EndsWith("_panoptic-fused-colored-mask.png")))
        {
            string dest = Path.Combine(PathConfig.GetPackagedAnnotatedImagesFolder(PackageName), Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        // After copying all annotated images
        var legend = ConfigLoader.GetLabels(ConfigLoader.LoadDefaultAnnotationConfig());

        foreach (string file in Directory.GetFiles(PathConfig.GetPackagedAnnotatedImagesFolder(PackageName)))
        {
            if (!file.EndsWith("_panoptic-fused-colored-mask.png")) continue;

            byte[] bytes = File.ReadAllBytes(file);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            tex.name = Path.GetFileNameWithoutExtension(file);

            var data = AnnotationAnalyzer.AnalyzeImage(tex, legend);

            string jsonPath = Path.Combine(PathConfig.GetPackagedDataFolder(PackageName), tex.name + "_analysis.json");
            AnnotationAnalyzer.SaveToJson(data, jsonPath);
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