using UnityEngine;
using SFB;
using System.IO;
using System.Diagnostics;
using System;
using UnityEngine.Video;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Text;

public class ImageProcesser : MonoBehaviour
{
    public static event Action OnProcessSelectSingleImage;
    public static event Action OnProcessSelectFolderOfImages;
    public static event Action OnSelectVideoAndProcessFrames;

    // === Progress tracking events ===
    public static event Action<int, int> OnProcessingProgress; // (current, total)

    private static int currentImageIndex = 0;
    private static int totalImages = 0;

    public static void RequestProcessSingleImage() => OnProcessSelectSingleImage?.Invoke();
    public static void RequestProcessFolderOfImages() => OnProcessSelectFolderOfImages?.Invoke();
    public static void RequestProcessVideoFrames() => OnSelectVideoAndProcessFrames?.Invoke();

    int amountOfImagesToProcessVideo = 4;

    // Request current progress info (for polling if needed)
    public static void GetCurrentProgress(out int current, out int total)
    {
        current = currentImageIndex;
        total = totalImages;
    }

    public enum ProcessingType
    {
        Annotation,
        AnomalyDetection
    }

    private ProcessingType processingType = ProcessingType.Annotation;

    // Reference to the packager to get the package name
    private ProcessingResultPackager packager;

    private void OnEnable()
    {
        OnProcessSelectSingleImage += ProcessSelectSingleImage;
        OnProcessSelectFolderOfImages += ProcessSelectFolderOfImages;
        OnSelectVideoAndProcessFrames += SelectVideoAndProcessFrames;
    }

    private void OnDisable()
    {
        OnProcessSelectSingleImage -= ProcessSelectSingleImage;
        OnProcessSelectFolderOfImages -= ProcessSelectFolderOfImages;
        OnSelectVideoAndProcessFrames -= SelectVideoAndProcessFrames;
    }

    void Start()
    {
        // Initialize all directories through the centralized config
        PathConfig.InitializeDirectories();

        // Get reference to packager
        packager = GetComponent<ProcessingResultPackager>();
        if (packager == null)
        {
            packager = FindObjectOfType<ProcessingResultPackager>();
        }
    }

    public void ChangeAmountOfImagesToProcessVideo(int newAmount)
    {
        amountOfImagesToProcessVideo = newAmount;
    }

    public void ProcessSelectSingleImage()
    {
        var extensions = new[] { new SFB.ExtensionFilter("Image Files", "png", "jpg", "jpeg") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select an image", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string selectedFile = paths[0];
            string destPath = Path.Combine(PathConfig.UsedImagesFolder, Path.GetFileName(selectedFile));
            File.Copy(selectedFile, destPath, true);
            UnityEngine.Debug.Log($"Copied {selectedFile} to folder: {destPath}");

            // Start async processing
            StartCoroutine(ProcessSingleImageAsync(destPath));
        }
    }

    private IEnumerator ProcessSingleImageAsync(string imagePath)
    {
        UIManager.RequestUIChange(UIManager.UIType.ProcessingImages);

        // Initialize package folders once at the start
        InitializePackageFolders();

        // Set progress for single image
        currentImageIndex = 1;
        totalImages = 1;
        OnProcessingProgress?.Invoke(currentImageIndex, totalImages);

        yield return StartCoroutine(ProcessImageWithAI(imagePath));

        // Package this single image immediately after processing
        yield return StartCoroutine(PackageSingleImageData(imagePath));

        UIManager.RequestUIChange(UIManager.UIType.DoneProcessing);
    }

    public void ProcessSelectFolderOfImages()
    {
        string[] folderPaths = StandaloneFileBrowser.OpenFolderPanel("Select a folder with images", "", false);
        if (folderPaths == null || folderPaths.Length == 0 || string.IsNullOrEmpty(folderPaths[0]))
            return;

        string folderPath = folderPaths[0];

        string[] imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
        var validExtensions = new string[] { ".png", ".jpg", ".jpeg" };
        imageFiles = Array.FindAll(imageFiles, f => Array.Exists(validExtensions, ext => ext.Equals(Path.GetExtension(f), StringComparison.OrdinalIgnoreCase)));

        if (imageFiles.Length == 0)
        {
            UnityEngine.Debug.LogWarning("No images found in selected folder.");
            return;
        }

        // Copy images to imagesFolder before processing
        string[] copiedImagePaths = new string[imageFiles.Length];
        for (int i = 0; i < imageFiles.Length; i++)
        {
            string destPath = Path.Combine(PathConfig.UsedImagesFolder, Path.GetFileName(imageFiles[i]));
            File.Copy(imageFiles[i], destPath, true);
            copiedImagePaths[i] = destPath;
            UnityEngine.Debug.Log($"Copied {imageFiles[i]} to folder: {destPath}");
        }

        // Start async processing with copied files
        StartCoroutine(ProcessFolderImagesAsync(copiedImagePaths));
    }

    private IEnumerator ProcessFolderImagesAsync(string[] imageFiles)
    {
        UIManager.RequestUIChange(UIManager.UIType.ProcessingImages);

        // Initialize package folders once at the start
        InitializePackageFolders();

        yield return StartCoroutine(ProcessImageListAsync(imageFiles));
        UIManager.RequestUIChange(UIManager.UIType.DoneProcessing);
    }

    private IEnumerator ProcessImageListAsync(string[] imageFiles)
    {
        // Initialize progress tracking
        totalImages = imageFiles.Length;
        currentImageIndex = 0;

        for (int i = 0; i < imageFiles.Length; i++)
        {
            currentImageIndex = i + 1;
            OnProcessingProgress?.Invoke(currentImageIndex, totalImages);

            UnityEngine.Debug.Log($"[AI] Processing {Path.GetFileName(imageFiles[i])} ({currentImageIndex}/{totalImages})");
            yield return StartCoroutine(ProcessImageWithAI(imageFiles[i]));

            // Package this image immediately after processing
            yield return StartCoroutine(PackageSingleImageData(imageFiles[i]));
        }

        UnityEngine.Debug.Log("AI processing completed for all images.");
    }

    public void SelectVideoAndProcessFrames()
    {
        var extensions = new[] { new SFB.ExtensionFilter("Video Files", "mp4", "mov", "avi") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select a video", "", extensions, false);
        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            return;

        string videoPath = paths[0];

        StartCoroutine(ProcessVideoFrames(videoPath, amountOfImagesToProcessVideo));
    }

    private IEnumerator WaitForFramesFolder(string folder, int expectedFrameCount, float timeoutSeconds = 30f)
    {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < timeoutSeconds)
        {
            if (Directory.Exists(folder))
            {
                var files = Directory.GetFiles(folder, "frame_*.png");
                if (files != null && files.Length >= expectedFrameCount)
                {
                    UnityEngine.Debug.Log($"Found {files.Length} frames (expected {expectedFrameCount})");
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }

        // Timeout reached
        int currentCount = Directory.Exists(folder) ? Directory.GetFiles(folder, "frame_*.png").Length : 0;
        UnityEngine.Debug.LogWarning($"Timeout waiting for frames. Found {currentCount}/{expectedFrameCount} frames.");
    }

    private IEnumerator RunProcessAsync(ProcessStartInfo startInfo, string workingDir = null)
    {
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        if (!string.IsNullOrEmpty(workingDir))
            startInfo.WorkingDirectory = workingDir;

        var stdout = new List<string>();
        var stderr = new List<string>();

        using (var proc = new Process())
        {
            proc.StartInfo = startInfo;
            proc.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) lock (stdout) stdout.Add(e.Data); };
            proc.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) lock (stderr) stderr.Add(e.Data); };

            try
            {
                proc.Start();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to start process {startInfo.FileName}: {ex}");
                yield break;
            }

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            while (!proc.HasExited)
                yield return null;

            lock (stdout)
            {
                for (int i = 0; i < Math.Min(20, stdout.Count); i++)
                    UnityEngine.Debug.Log($"[proc stdout] {stdout[i]}");
            }
            lock (stderr)
            {
                for (int i = 0; i < Math.Min(20, stderr.Count); i++)
                    UnityEngine.Debug.LogWarning($"[proc stderr] {stderr[i]}");
            }

            UnityEngine.Debug.Log($"Process {startInfo.FileName} exited with code {proc.ExitCode}");
        }
    }

    private IEnumerator ProcessVideoFrames(string videoPath, int desiredFrames)
    {
        UIManager.RequestUIChange(UIManager.UIType.ProcessingImages);

        // Initialize package folders once at the start
        InitializePackageFolders();

        // Split video into frames first
        var vpGO = new GameObject("TempVP");
        var vp = vpGO.AddComponent<VideoPlayer>();
        vp.url = videoPath;
        vp.Prepare();
        while (!vp.isPrepared) yield return null;
        double duration = vp.length;
        Destroy(vpGO);

        double fps = Math.Max(0.0001, desiredFrames / duration);
        string batchPath = Path.Combine(PathConfig.FrameExtractorPath, "extract_frames.bat");
        if (!File.Exists(batchPath))
        {
            UnityEngine.Debug.LogError($"extract_frames.bat not found at {batchPath}");
            UIManager.RequestUIChange(UIManager.UIType.DoneProcessing);
            yield break;
        }

        var startInfo = new ProcessStartInfo()
        {
            FileName = batchPath,
            Arguments = $"\"{videoPath}\" {fps} \"{PathConfig.UsedImagesFolder}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        string workingDir = Path.GetDirectoryName(batchPath);

        // Extract frames and wait for completion
        UnityEngine.Debug.Log("Extracting frames from video...");
        yield return StartCoroutine(RunProcessAsync(startInfo, workingDir));
        yield return StartCoroutine(WaitForFramesFolder(PathConfig.UsedImagesFolder, desiredFrames, 30f));

        // Now get all extracted frames
        string[] allFrames = Directory.GetFiles(PathConfig.UsedImagesFolder, "frame_*.png")
                                     .OrderBy(x => x).ToArray();

        if (allFrames.Length == 0)
        {
            UnityEngine.Debug.LogWarning("No frames found in folder after extraction!");
            UIManager.RequestUIChange(UIManager.UIType.DoneProcessing);
            yield break;
        }

        UnityEngine.Debug.Log($"Found {allFrames.Length} frames. Selecting {desiredFrames} for processing...");

        // Select frames to process
        string[] framesToProcess;
        if (desiredFrames >= allFrames.Length)
        {
            framesToProcess = allFrames;
        }
        else
        {
            framesToProcess = new string[desiredFrames];
            float step = (allFrames.Length - 1) / (float)(desiredFrames - 1);
            for (int i = 0; i < desiredFrames; i++)
                framesToProcess[i] = allFrames[Mathf.RoundToInt(i * step)];
        }

        // Use the same processing loop as folder processing
        yield return StartCoroutine(ProcessImageListAsync(framesToProcess));

        UIManager.RequestUIChange(UIManager.UIType.DoneProcessing);
    }

    private IEnumerator ProcessImageWithAI(string imagePath)
    {
        switch (processingType)
        {
            case ProcessingType.Annotation:
                yield return StartCoroutine(RunAnnotationAI(imagePath));
                break;
            case ProcessingType.AnomalyDetection:
                yield return StartCoroutine(RunAnomalyDetectionAI(imagePath));
                break;
        }
    }

    private IEnumerator RunAnnotationAI(string imagePath)
    {
        string batFile = Path.Combine(PathConfig.AIPackagePath, "run_segmentation.bat");

        ProcessStartInfo psi = new ProcessStartInfo()
        {
            FileName = batFile,
            Arguments = $"-i \"{imagePath}\" -o \"{PathConfig.AnnotatedImagesFolder}\" -s -p",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        yield return StartCoroutine(RunProcessAsync(psi, null));

        UnityEngine.Debug.Log("AI processing completed for image. Check results folder.");
    }

    private IEnumerator RunAnomalyDetectionAI(string imagePath)
    {
        // Placeholder for future Anomaly Detection logic
        UnityEngine.Debug.Log($"Anomaly Detection would process: {imagePath}");
        yield return null;
    }

    /// <summary>
    /// Initializes the package folder structure once at the start of processing.
    /// </summary>
    private void InitializePackageFolders()
    {
        if (packager == null)
        {
            UnityEngine.Debug.LogWarning("No ProcessingResultPackager found. Cannot initialize package folders.");
            return;
        }

        string packageName = packager.PackageName;
        if (string.IsNullOrEmpty(packageName))
            packageName = "UnnamedPackage";

        // Create all package directories at once
        PathConfig.CreatePackagedDataFolders(packageName);

        UnityEngine.Debug.Log($"Initialized package folders for: {packageName}");
    }

    /// <summary>
    /// Packages a single image immediately after processing.
    /// Copies the source image and its annotated results, then generates JSON.
    /// </summary>
    private IEnumerator PackageSingleImageData(string originalImagePath)
    {
        if (packager == null)
        {
            UnityEngine.Debug.LogWarning("No ProcessingResultPackager found. Skipping packaging.");
            yield break;
        }

        string packageName = packager.PackageName;
        if (string.IsNullOrEmpty(packageName))
            packageName = "UnnamedPackage";

        // Note: Package directories are already created by InitializePackageFolders()
        // No need to call CreatePackagedDataFolders again

        string imageFileName = Path.GetFileName(originalImagePath);
        string baseNameNoExt = Path.GetFileNameWithoutExtension(imageFileName);

        // Copy the used image
        string usedImageDest = Path.Combine(PathConfig.GetPackagedUsedImagesFolder(packageName), imageFileName);
        if (File.Exists(originalImagePath))
        {
            File.Copy(originalImagePath, usedImageDest, true);
        }

        // Copy annotated images (colored mask, panoptic mask, etc.)
        string[] annotatedFiles = Directory.GetFiles(PathConfig.AnnotatedImagesFolder, $"{baseNameNoExt}*.*");
        foreach (string annotatedFile in annotatedFiles)
        {
            string annotatedDest = Path.Combine(PathConfig.GetPackagedAnnotatedImagesFolder(packageName), Path.GetFileName(annotatedFile));
            File.Copy(annotatedFile, annotatedDest, true);
        }

        // Load the legend
        var legend = ConfigLoader.GetLabels(ConfigLoader.LoadDefaultAnnotationConfig());

        // Find and process the colored mask
        string coloredMaskPath = Path.Combine(PathConfig.GetPackagedAnnotatedImagesFolder(packageName),
                                              $"{baseNameNoExt}_colored-mask.png");

        if (File.Exists(coloredMaskPath))
        {
            string panopticMaskPath = Path.Combine(PathConfig.GetPackagedAnnotatedImagesFolder(packageName),
                                                   $"{baseNameNoExt}_panoptic-colored-mask.png");

            // Load colored mask
            byte[] coloredBytes = File.ReadAllBytes(coloredMaskPath);
            Texture2D coloredTex = new Texture2D(2, 2);
            coloredTex.LoadImage(coloredBytes);
            coloredTex.name = baseNameNoExt;

            // Load panoptic mask (if it exists)
            Texture2D panopticTex = null;
            if (File.Exists(panopticMaskPath))
            {
                byte[] panoBytes = File.ReadAllBytes(panopticMaskPath);
                panopticTex = new Texture2D(2, 2);
                panopticTex.LoadImage(panoBytes);
            }

            // Analyze and save JSON
            var data = AnnotationAnalyzer.AnalyzeImage(coloredTex, legend, panopticTex);
            AnnotationAnalyzer.SaveToJson(data, packageName);

            UnityEngine.Debug.Log($"Packaged single image: {imageFileName}");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"Colored mask not found for {imageFileName}. Skipping JSON generation.");
        }

        yield return null;
    }
}