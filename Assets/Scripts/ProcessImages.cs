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
    // === External Package Paths (relative to build/project root) ===
    private string aiPackagePath;
    private string frameExtractorPath;

    // === Output folders (constant relative path) ===
    private const string OUTPUT_FOLDER_NAME = "Output";

    private string baseFolder;
    private string imagesFolder;
    private string resultsFolder;

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
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        aiPackagePath = Path.Combine(root, "ExternalPackages", "Segmentation");
        frameExtractorPath = Path.Combine(root, "ExternalPackages", "frame_extractor");

        baseFolder = Path.Combine(root, OUTPUT_FOLDER_NAME);
        imagesFolder = Path.Combine(baseFolder, "ImagesUsed");
        resultsFolder = Path.Combine(baseFolder, "Results");

        Directory.CreateDirectory(imagesFolder);
        Directory.CreateDirectory(resultsFolder);

        UnityEngine.Debug.Log($"Using folders:\nImages: {imagesFolder}\nResults: {resultsFolder}");
    }

    public void ChangeAmountOfImagesToProcessVideo (int newAmount)
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
            string destPath = Path.Combine(imagesFolder, Path.GetFileName(selectedFile));
            File.Copy(selectedFile, destPath, true);
            UnityEngine.Debug.Log($"Copied {selectedFile} to folder: {destPath}");

            // Start async processing
            StartCoroutine(ProcessSingleImageAsync(destPath));
        }
    }

    private IEnumerator ProcessSingleImageAsync(string imagePath)
    {
        UIManager.RequestUIChange(UIManager.UIType.ProcessingImages);

        // Set progress for single image
        currentImageIndex = 1;
        totalImages = 1;
        OnProcessingProgress?.Invoke(currentImageIndex, totalImages);

        yield return StartCoroutine(ProcessImageWithAI(imagePath));

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

        // Start async processing
        StartCoroutine(ProcessFolderImagesAsync(imageFiles));
    }

    private IEnumerator ProcessFolderImagesAsync(string[] imageFiles)
    {
        UIManager.RequestUIChange(UIManager.UIType.ProcessingImages);
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
            yield return new WaitForSeconds(0.5f); // Check every half second instead of every frame
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

            // Revert to original: yield return null
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

        // Split video into frames first
        var vpGO = new GameObject("TempVP");
        var vp = vpGO.AddComponent<VideoPlayer>();
        vp.url = videoPath;
        vp.Prepare();
        while (!vp.isPrepared) yield return null;
        double duration = vp.length;
        Destroy(vpGO);

        double fps = Math.Max(0.0001, desiredFrames / duration);
        string batchPath = Path.Combine(frameExtractorPath, "extract_frames.bat");
        if (!File.Exists(batchPath))
        {
            UnityEngine.Debug.LogError($"extract_frames.bat not found at {batchPath}");
            UIManager.RequestUIChange(UIManager.UIType.DoneProcessing);
            yield break;
        }

        var startInfo = new ProcessStartInfo()
        {
            FileName = batchPath,
            Arguments = $"\"{videoPath}\" {fps} \"{imagesFolder}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        string workingDir = Path.GetDirectoryName(batchPath);

        // Extract frames and wait for completion
        UnityEngine.Debug.Log("Extracting frames from video...");
        yield return StartCoroutine(RunProcessAsync(startInfo, workingDir));
        yield return StartCoroutine(WaitForFramesFolder(imagesFolder, desiredFrames, 30f));

        // Now get all extracted frames
        string[] allFrames = Directory.GetFiles(imagesFolder, "frame_*.png")
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
        string batFile = Path.Combine(aiPackagePath, "run_segmentation.bat");

        ProcessStartInfo psi = new ProcessStartInfo()
        {
            FileName = batFile,
            Arguments = $"-i \"{imagePath}\" -o \"{resultsFolder}\" -s -p",
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
}