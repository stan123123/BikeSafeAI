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

/// <summary>
/// THIS SCRIPT IS OLD, DEPRACATED AND NOT USED, IT WAS A WIP PROCESSING SCRIPT IN EARLY PRODUCTION
/// </summary>

public class CS_MenuSelectFile : MonoBehaviour
{
    // === External Package Paths (relative to build/project root) ===
    private string aiPackagePath;
    private string frameExtractorPath;

    // === Output folders (constant relative path) ===
    private const string OUTPUT_FOLDER_NAME = "Output";

    private string baseFolder;
    private string imagesFolder;
    private string resultsFolder;

    void Start()
    {
        // Root folder is always one level above Application.dataPath
        // Editor: <ProjectFolder>
        // Build:  <BuildFolder> (where the .exe lives)
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        // External package locations
        aiPackagePath = Path.Combine(root, "ExternalPackages", "Segmentation");
        frameExtractorPath = Path.Combine(root, "ExternalPackages", "frame_extractor");

        // Output folder
        baseFolder = Path.Combine(root, OUTPUT_FOLDER_NAME);
        imagesFolder = Path.Combine(baseFolder, "ImagesUsed");
        resultsFolder = Path.Combine(baseFolder, "Results");

        // Create subfolders
        Directory.CreateDirectory(imagesFolder);
        Directory.CreateDirectory(resultsFolder);

        UnityEngine.Debug.Log($"Using folders:\nImages: {imagesFolder}\nResults: {resultsFolder}");
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

            // Call the AI package
            string batFile = Path.Combine(aiPackagePath, "run_segmentation.bat");

            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = batFile,
                Arguments = $"-i \"{destPath}\" -o \"{resultsFolder}\" -s -p",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process proc = new Process();
            proc.StartInfo = psi;
            proc.OutputDataReceived += (sender, e) => { if (e.Data != null) UnityEngine.Debug.Log(e.Data); };
            proc.ErrorDataReceived += (sender, e) => { if (e.Data != null) UnityEngine.Debug.LogError(e.Data); };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();

            UnityEngine.Debug.Log("AI processing completed. Check results folder.");
        }
    }

    public void ProcessSelectFolderOfImages()
    {
        // Let the user pick a folder
        string[] folderPaths = StandaloneFileBrowser.OpenFolderPanel("Select a folder with images", "", false);
        if (folderPaths == null || folderPaths.Length == 0 || string.IsNullOrEmpty(folderPaths[0]))
            return;

        string folderPath = folderPaths[0];

        // Get all image files in the folder
        string[] imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
        var validExtensions = new string[] { ".png", ".jpg", ".jpeg" };
        imageFiles = Array.FindAll(imageFiles, f => Array.Exists(validExtensions, ext => ext.Equals(Path.GetExtension(f), StringComparison.OrdinalIgnoreCase)));

        if (imageFiles.Length == 0)
        {
            UnityEngine.Debug.LogWarning("No images found in selected folder.");
            return;
        }

        // Process each image
        foreach (var framePath in imageFiles)
        {
            UnityEngine.Debug.Log($"[AI] Processing {Path.GetFileName(framePath)}");

            string batFile = Path.Combine(aiPackagePath, "run_segmentation.bat");

            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = batFile,
                Arguments = $"-i \"{framePath}\" -o \"{resultsFolder}\" -s -p",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process proc = new Process();
            proc.StartInfo = psi;
            proc.OutputDataReceived += (sender, e) => { if (e.Data != null) UnityEngine.Debug.Log(e.Data); };
            proc.ErrorDataReceived += (sender, e) => { if (e.Data != null) UnityEngine.Debug.LogError(e.Data); };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit(); // block until done
        }

        UnityEngine.Debug.Log("AI processing completed for all images in folder.");
    }

    public void SelectVideoAndProcessFrames()
    {
        int desiredFrames = 8;

        var extensions = new[] { new SFB.ExtensionFilter("Video Files", "mp4", "mov", "avi") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select a video", "", extensions, false);
        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            return;

        string videoPath = paths[0];
        StartCoroutine(ProcessVideoFrames(videoPath, desiredFrames));
    }

    private IEnumerator WaitForFramesFolder(string folder, float timeoutSeconds = 10f)
    {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < timeoutSeconds)
        {
            if (Directory.Exists(folder))
            {
                var files = Directory.GetFiles(folder, "frame_*.png");
                if (files != null && files.Length > 0)
                    yield break; // frames found
            }
            yield return null;
        }
        yield break;
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
        var vpGO = new GameObject("TempVP");
        var vp = vpGO.AddComponent<VideoPlayer>();
        vp.url = videoPath;
        vp.Prepare();
        while (!vp.isPrepared) yield return null;
        double duration = vp.length;
        Destroy(vpGO);

        double fps = Math.Max(0.0001, desiredFrames / duration);
        UnityEngine.Debug.Log($"Requesting ffmpeg fps={fps} (desired frames={desiredFrames}, duration={duration}s)");

        string batchPath = Path.Combine(frameExtractorPath, "extract_frames.bat");
        if (!File.Exists(batchPath))
        {
            UnityEngine.Debug.LogError($"extract_frames.bat not found at {batchPath}");
            yield break;
        }

        var startInfo = new ProcessStartInfo()
        {
            FileName = batchPath,
            Arguments = $"\"{videoPath}\" {fps} \"{imagesFolder}\""
        };

        string workingDir = Path.GetDirectoryName(batchPath);
        yield return StartCoroutine(RunProcessAsync(startInfo, workingDir));

        yield return StartCoroutine(WaitForFramesFolder(imagesFolder, 8f));

        string[] allFrames = Directory.GetFiles(imagesFolder, "frame_*.png")
                                     .OrderBy(x => x).ToArray();

        if (allFrames.Length == 0)
        {
            UnityEngine.Debug.LogWarning("No frames found in folder after extraction!");
            yield break;
        }

        string[] framesToProcess;
        if (desiredFrames >= allFrames.Length)
            framesToProcess = allFrames;
        else
        {
            framesToProcess = new string[desiredFrames];
            float step = (allFrames.Length - 1) / (float)(desiredFrames - 1);
            for (int i = 0; i < desiredFrames; i++)
                framesToProcess[i] = allFrames[Mathf.RoundToInt(i * step)];
        }

        UnityEngine.Debug.Log($"Frames to process: {framesToProcess.Length} (found {allFrames.Length})");

        foreach (var framePath in framesToProcess)
        {
            UnityEngine.Debug.Log($"[AI] Processing {Path.GetFileName(framePath)}");

            string batFile = Path.Combine(aiPackagePath, "run_segmentation.bat");

            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = batFile,
                Arguments = $"-i \"{framePath}\" -o \"{resultsFolder}\" -s -p",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process proc = new Process();
            proc.StartInfo = psi;
            proc.OutputDataReceived += (sender, e) => { if (e.Data != null) UnityEngine.Debug.Log(e.Data); };
            proc.ErrorDataReceived += (sender, e) => { if (e.Data != null) UnityEngine.Debug.LogError(e.Data); };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
        }

        UnityEngine.Debug.Log("AI processing completed for all selected frames.");
    }
}