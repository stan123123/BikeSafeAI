using UnityEngine;
using System.IO;

/// <summary>
/// Centralized configuration for all folder paths used in the application.
/// </summary>
public static class PathConfig
{
    // Root paths
    private static string _projectRoot;
    public static string ProjectRoot
    {
        get
        {
            if (string.IsNullOrEmpty(_projectRoot))
                _projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return _projectRoot;
        }
    }

    // External package paths
    public static string AIPackagePath => Path.Combine(ProjectRoot, "ExternalPackages", "Segmentation");
    public static string FrameExtractorPath => Path.Combine(ProjectRoot, "ExternalPackages", "frame_extractor");

    // Output folder paths
    private const string OUTPUT_FOLDER_NAME = "Output";
    public static string OutputFolder => Path.Combine(ProjectRoot, OUTPUT_FOLDER_NAME);

    // Temp folder names
    private const string USED_IMAGES_FOLDER_NAME = "TEMP_ImagesUsed";
    private const string ANNOTATED_IMAGES_FOLDER_NAME = "TEMP_AnnotatedImages";

    // Full paths for temp folders
    public static string UsedImagesFolder => Path.Combine(OutputFolder, USED_IMAGES_FOLDER_NAME);
    public static string AnnotatedImagesFolder => Path.Combine(OutputFolder, ANNOTATED_IMAGES_FOLDER_NAME);

    /// <summary>
    /// Initialize all required temp directories. Call this once at application start.
    /// </summary>
    public static void InitializeDirectories()
    {
        Directory.CreateDirectory(UsedImagesFolder);
        Directory.CreateDirectory(AnnotatedImagesFolder);

        Debug.Log($"Initialized temp folders:\n" +
                  $"Used Images: {UsedImagesFolder}\n" +
                  $"Annotated Images: {AnnotatedImagesFolder}");
    }

    /// <summary>
    /// Creates a packaged data folder structure for a named batch.
    /// Returns the root folder of the package.
    /// </summary>
    /// <param name="packageName">Name of the batch/package.</param>
    public static string CreatePackagedDataFolders(string packageName)
    {
        if (string.IsNullOrEmpty(packageName))
            packageName = "UnnamedPackage";

        // Root folder for this package
        string packageRoot = Path.Combine(OutputFolder, "packagedData", packageName);

        // Subfolders
        string usedImages = Path.Combine(packageRoot, "usedImages");
        string annotatedImages = Path.Combine(packageRoot, "annotatedImages");

        // Create all directories
        Directory.CreateDirectory(packageRoot);
        Directory.CreateDirectory(usedImages);
        Directory.CreateDirectory(annotatedImages);

        Debug.Log($"Created packaged data folders for batch '{packageName}':\n" +
                  $"Package Root: {packageRoot}\n" +
                  $"Used Images: {usedImages}\n" +
                  $"Annotated Images: {annotatedImages}");

        return packageRoot;
    }

    /// <summary>
    /// Convenience getters if you know the package name
    /// </summary>
    public static string GetPackagedUsedImagesFolder(string packageName) => Path.Combine(OutputFolder, "packagedData", packageName, "usedImages");
    public static string GetPackagedAnnotatedImagesFolder(string packageName) => Path.Combine(OutputFolder, "packagedData", packageName, "annotatedImages");
}