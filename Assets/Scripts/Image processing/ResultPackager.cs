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

        // Process only *_colored-mask.png images
        foreach (string coloredMaskPath in Directory.GetFiles(PathConfig.GetPackagedAnnotatedImagesFolder(PackageName))
                                                     .Where(f => f.EndsWith("_colored-mask.png")))
        {
            string baseName = Path.GetFileNameWithoutExtension(coloredMaskPath).Replace("_colored-mask", "");
            string panopticMaskPath = Path.Combine(PathConfig.GetPackagedAnnotatedImagesFolder(PackageName),
                                                   baseName + "_panoptic-colored-mask.png");

            // Load colored mask
            byte[] coloredBytes = File.ReadAllBytes(coloredMaskPath);
            Texture2D coloredTex = new Texture2D(2, 2);
            coloredTex.LoadImage(coloredBytes);
            coloredTex.name = Path.GetFileNameWithoutExtension(coloredMaskPath);

            // Load panoptic mask (if it exists)
            Texture2D panopticTex = null;
            if (File.Exists(panopticMaskPath))
            {
                byte[] panoBytes = File.ReadAllBytes(panopticMaskPath);
                panopticTex = new Texture2D(2, 2);
                panopticTex.LoadImage(panoBytes);
            }

            var data = AnnotationAnalyzer.AnalyzeImage(coloredTex, legend, panopticTex);

            // FIXED: Pass only the PackageName, not the full path
            AnnotationAnalyzer.SaveToJson(data, PackageName);
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