using UnityEngine;
using TMPro;
using UnityEngine.Video;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Main manager of review data, basically generates all points of interest that we need for reviewing data stuff
/// TODO: refactor this stuff into different helper static classes
/// </summary>
public class DataReviewManager : MonoBehaviour
{
    // Loaded data
    private ProcessedVideoData _videoData;
    private List<ImageAnnotationData> _frameAnalysisData = new List<ImageAnnotationData>();
    private List<PointOfInterest> _pointsOfInterest = new List<PointOfInterest>();

    public static event Action<List<PointOfInterest>, ProcessedVideoData> OnDataProcessingCompleted;

    // Configuration for POI generation
    [Header("POI Generation Settings")]
    [SerializeField] private int _minConsecutiveFramesForPOI = 3; // Minimum frames to form a POI
    [SerializeField] private int _maxGapBetweenPOIFrames = 2; // Max gap in frames before splitting POIs

    private Dictionary<string, string> _labelLookupTable = new Dictionary<string, string>();

    private void OnEnable()
    {
        Invoke(nameof(LoadAndProcessData), .1f); // idk i tried many different ways but this is the only fix to the race condition
    }

    /// <summary>
    /// Main pipeline: Load video data, frame analyses, and generate POIs
    /// </summary>
    private void LoadAndProcessData()
    {
        InitializeLabelLookup();

        string folderPath = ReviewUserSelectionManager.SelectedPath;

        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"Folder not found: {folderPath}");
            return;
        }

        // Step 1: Load ProcessedVideoData.json
        if (!LoadVideoData(folderPath))
        {
            Debug.LogError("Failed to load video data. Aborting.");
            return;
        }

        // Step 2: Load all frame analysis JSONs
        if (!LoadFrameAnalysisData(folderPath))
        {
            Debug.LogError("Failed to load frame analysis data. Aborting.");
            return;
        }

        // Step 3: Generate Points of Interest based on filters
        GeneratePointsOfInterest();

        Debug.Log("[DataReviewManager] Invoking OnDataProcessingCompleted with " + _pointsOfInterest.Count + " points");
        OnDataProcessingCompleted?.Invoke(_pointsOfInterest, _videoData);

        // Step 4: Log results
        LogPOIResults();
    }

    /// <summary>
    /// quick hack because not enough time, all of this should really be happening in a static helper script :(((((
    /// </summary>
    private void InitializeLabelLookup()
    {
        _labelLookupTable.Clear();

        ConfigData config = ConfigLoader.LoadDefaultAnnotationConfig();

        if (config != null && config.labels != null)
        {
            foreach (LabelData label in config.labels)
            {
                if (!_labelLookupTable.ContainsKey(label.name))
                {
                    _labelLookupTable.Add(label.name, label.readable);
                }
            }
            Debug.Log($"Initialized Label Lookup with {_labelLookupTable.Count} entries.");
        }
    }

    /// <summary>
    /// Loads ProcessedVideoData.json from the selected folder
    /// </summary>
    private bool LoadVideoData(string folderPath)
    {
        string videoDataPath = Path.Combine(folderPath, "ProcessedVideoData.json");

        if (!File.Exists(videoDataPath))
        {
            Debug.LogError($"ProcessedVideoData.json not found at: {videoDataPath}");
            return false;
        }

        try
        {
            string jsonContent = File.ReadAllText(videoDataPath);
            _videoData = JsonUtility.FromJson<ProcessedVideoData>(jsonContent);

            Debug.Log($"Loaded Video Data: {_videoData.totalFrames} frames, " +
                      $"{_videoData.videoDurationSeconds}s duration, " +
                      $"GPS: {_videoData.hasGPSData}");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse ProcessedVideoData.json: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Loads all frame analysis JSON files from imageStatsJson folder
    /// </summary>
    private bool LoadFrameAnalysisData(string folderPath)
    {
        string imageStatsPath = Path.Combine(folderPath, PathConfig.ImageStatsJsonFolderName);

        if (!Directory.Exists(imageStatsPath))
        {
            Debug.LogError($"imageStatsJson folder not found at: {imageStatsPath}");
            return false;
        }

        string[] jsonFiles = Directory.GetFiles(imageStatsPath, "*_analysis.json");

        if (jsonFiles.Length == 0)
        {
            Debug.LogError($"No analysis JSON files found in: {imageStatsPath}");
            return false;
        }

        Debug.Log($"Found {jsonFiles.Length} frame analysis files. Loading...");

        _frameAnalysisData.Clear();

        Array.Sort(jsonFiles, (a, b) =>
        {
            int frameA = ExtractFrameNumber(a);
            int frameB = ExtractFrameNumber(b);
            return frameA.CompareTo(frameB);
        });

        foreach (string jsonFile in jsonFiles)
        {
            try
            {
                string jsonContent = File.ReadAllText(jsonFile);
                ImageAnnotationData frameData = JsonUtility.FromJson<ImageAnnotationData>(jsonContent);
                _frameAnalysisData.Add(frameData);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load {Path.GetFileName(jsonFile)}: {e.Message}");
            }
        }

        Debug.Log($"Successfully loaded {_frameAnalysisData.Count} frame analysis files");
        return _frameAnalysisData.Count > 0;
    }

    /// <summary>
    /// Extracts frame number from filename (e.g., "frame_00001_analysis.json" -> 1)
    /// </summary>
    private int ExtractFrameNumber(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        // Assuming format: frame_XXXXX_analysis
        string[] parts = fileName.Split('_');
        if (parts.Length >= 2 && int.TryParse(parts[1], out int frameNum))
        {
            return frameNum;
        }
        return 0;
    }

    /// <summary>
    /// Main POI generation logic - filters frames and groups consecutive matches
    /// </summary>
    private void GeneratePointsOfInterest()
    {
        _pointsOfInterest.Clear();

        if (_frameAnalysisData.Count == 0)
        {
            Debug.LogWarning("No frame data to analyze for POIs");
            return;
        }

        // Debug: Log filter settings
        Debug.Log($"=== Filter Settings ===");
        Debug.Log($"Included Labels: {string.Join(", ", ReviewUserSelectionManager.IncludedLabels)}");
        Debug.Log($"Minimum Accuracy: {ReviewUserSelectionManager.MinimumDesiredAccuracy}%");
        Debug.Log($"Whole Video Area: {ReviewUserSelectionManager.bSelectedWholeVideoArea}");
        Debug.Log($"Active Label Filters: {ReviewUserSelectionManager.LabelFilters.Count}");
        foreach (var filter in ReviewUserSelectionManager.LabelFilters)
        {
            Debug.Log($"  - {filter.nameReadable}: Included={filter.bIsIncludedInData}, " +
                      $"Accuracy>={filter.percentageAccuracyFilter}%, " +
                      $"Count={filter.minAmountOfObjects}-{filter.maxAmountOfObjects}");
        }

        // Debug: Sample first frame to see label names
        if (_frameAnalysisData.Count > 0)
        {
            Debug.Log($"=== Sample Frame Labels (Frame 0) ===");
            foreach (var label in _frameAnalysisData[0].labels)
            {
                string readable = ConvertToReadableName(label.labelName);
                Debug.Log($"  Internal: '{label.labelName}' -> Readable: '{readable}' " +
                          $"(Count: {label.instanceCount}, Avg Score: {label.averageScore:F2})");
            }
        }

        List<int> matchingFrameIndices = new List<int>();

        // Step 1: Find all frames that match the filter criteria
        for (int i = 0; i < _frameAnalysisData.Count; i++)
        {
            if (FrameMatchesFilters(_frameAnalysisData[i]))
            {
                matchingFrameIndices.Add(i);
            }
        }

        Debug.Log($"Found {matchingFrameIndices.Count} frames matching filter criteria");

        if (matchingFrameIndices.Count == 0)
        {
            Debug.Log("No frames match the current filters. No POIs generated.");
            return;
        }

        // Step 2: Group consecutive frames into POIs
        GroupFramesIntoPOIs(matchingFrameIndices);

        Debug.Log($"Generated {_pointsOfInterest.Count} Points of Interest");
    }

    /// <summary>
    /// Checks if a frame matches all the user-defined filters
    /// </summary>
    private bool FrameMatchesFilters(ImageAnnotationData frameData)
    {
        // If no labels are included in the filter, nothing can match
        if (ReviewUserSelectionManager.IncludedLabels.Count == 0)
        {
            return false;
        }

        bool hasAnyMatchingLabel = false;

        // Check each label in the frame against the filters
        foreach (var labelStats in frameData.labels)
        {
            // Convert internal label name to readable name for comparison
            string readableName = ConvertToReadableName(labelStats.labelName);

            // Check if this label is in the included labels (by readable name)
            if (!ReviewUserSelectionManager.IncludedLabels.Contains(readableName))
            {
                continue; // Skip this label if not included
            }

            // Get the specific filter for this label
            LabelFilterData? filter = GetFilterForLabel(readableName);

            if (!filter.HasValue)
            {
                // Label is included but has no specific filter - use default checks
                if (CheckDefaultLabelCriteria(labelStats))
                {
                    hasAnyMatchingLabel = true;
                }
                continue;
            }

            // Apply the specific filter
            if (CheckLabelAgainstFilter(labelStats, filter.Value))
            {
                hasAnyMatchingLabel = true;
            }
        }

        return hasAnyMatchingLabel;
    }

    /// <summary>
    /// converts names to usable ones, this should really be happening in a static class but i dont have enough time for that right now.
    /// </summary>
    private string ConvertToReadableName(string internalName)
    {
        if (string.IsNullOrEmpty(internalName) || internalName.StartsWith("Unknown_Label_"))
        {
            return internalName;
        }

        if (_labelLookupTable.TryGetValue(internalName, out string officialReadable))
        {
            return officialReadable;
        }

        // we are guessing the name here, this edge case should really never happen unless the user swaps out the config mid processing, but screw it
        string[] parts = internalName.Split(new string[] { "--" }, StringSplitOptions.None);
        string lastPart = parts[parts.Length - 1];
        string readable = lastPart.Replace("-", " ");

        System.Globalization.TextInfo textInfo =
            new System.Globalization.CultureInfo("en-US", false).TextInfo;

        return textInfo.ToTitleCase(readable);
    }

    /// <summary>
    /// Gets the filter data for a specific label by readable name
    /// </summary>
    private LabelFilterData? GetFilterForLabel(string labelReadableName)
    {
        foreach (var filter in ReviewUserSelectionManager.LabelFilters)
        {
            if (filter.nameReadable == labelReadableName && filter.bIsIncludedInData)
            {
                return filter;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks label against default criteria (minimum accuracy only)
    /// </summary>
    private bool CheckDefaultLabelCriteria(LabelStats labelStats)
    {
        float minAccuracy = ReviewUserSelectionManager.MinimumDesiredAccuracy / 100f;

        // Check if average score meets minimum accuracy
        if (labelStats.averageScore < minAccuracy)
        {
            return false;
        }

        // Check screen area if not whole video selected
        if (!ReviewUserSelectionManager.bSelectedWholeVideoArea)
        {
            if (!CheckInstancesInScreenArea(labelStats))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if a label matches the specific filter criteria
    /// </summary>
    private bool CheckLabelAgainstFilter(LabelStats labelStats, LabelFilterData filter)
    {
        // Check accuracy threshold
        float accuracyThreshold = filter.percentageAccuracyFilter / 100f;
        if (labelStats.averageScore < accuracyThreshold)
        {
            return false;
        }

        // Check instance count range
        if (labelStats.instanceCount < filter.minAmountOfObjects ||
            labelStats.instanceCount > filter.maxAmountOfObjects)
        {
            return false;
        }

        // Check screen area if applicable
        if (!ReviewUserSelectionManager.bSelectedWholeVideoArea)
        {
            if (!CheckInstancesInScreenArea(labelStats))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if any instances are within the user-defined screen area
    /// </summary>
    private bool CheckInstancesInScreenArea(LabelStats labelStats)
    {
        Vector2Int bottomLeft = ReviewUserSelectionManager.BottomLeft;
        Vector2Int topRight = ReviewUserSelectionManager.BottomRight;

        // Create a rect from the two corners
        Rect screenArea = new Rect(
            bottomLeft.x,
            bottomLeft.y,
            topRight.x - bottomLeft.x,
            topRight.y - bottomLeft.y
        );

        // Check if any instance center is within this area
        foreach (var instance in labelStats.instances)
        {
            Vector2 center = new Vector2(instance.center.x, instance.center.y);

            if (screenArea.Contains(center))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Groups consecutive matching frames into Points of Interest
    /// </summary>
    private void GroupFramesIntoPOIs(List<int> matchingFrameIndices)
    {
        if (matchingFrameIndices.Count == 0) return;

        int currentStartIndex = matchingFrameIndices[0];
        int currentEndIndex = matchingFrameIndices[0];
        int lastFrameIndex = matchingFrameIndices[0];

        for (int i = 1; i < matchingFrameIndices.Count; i++)
        {
            int frameIndex = matchingFrameIndices[i];
            int gap = frameIndex - lastFrameIndex;

            // If gap is within tolerance, extend current POI
            if (gap <= _maxGapBetweenPOIFrames)
            {
                currentEndIndex = frameIndex;
            }
            else
            {
                // Gap too large - finalize current POI and start new one
                FinalizePOI(currentStartIndex, currentEndIndex);
                currentStartIndex = frameIndex;
                currentEndIndex = frameIndex;
            }

            lastFrameIndex = frameIndex;
        }

        // Don't forget the last POI
        FinalizePOI(currentStartIndex, currentEndIndex);
    }

    /// <summary>
    /// Creates and adds a POI if it meets minimum frame requirements
    /// </summary>
    private void FinalizePOI(int startIndex, int endIndex)
    {
        int frameCount = endIndex - startIndex + 1;

        // Only create POI if it meets minimum consecutive frames requirement
        if (frameCount < _minConsecutiveFramesForPOI)
        {
            return;
        }

        // Calculate start time based on frame index and video data
        float startTime = CalculateFrameTime(startIndex);

        PointOfInterest poi = new PointOfInterest(
            startIndex: startIndex,
            endIndex: endIndex,
            startTimeInVideoSeconds: startTime,
            amountOfFrames: frameCount,
            hasFinishedProcessingWithoutError: true
        );

        _pointsOfInterest.Add(poi);
    }

    /// <summary>
    /// Calculates the time in seconds for a given frame index
    /// </summary>
    private float CalculateFrameTime(int frameIndex)
    {
        if (_videoData.totalFrames <= 0) return 0f;

        // Use derived or user-input frame delta
        float frameDelta = _videoData.userInputFrameDeltaSeconds > 0
            ? _videoData.userInputFrameDeltaSeconds
            : _videoData.derivedFrameDeltaSeconds;

        return frameIndex * frameDelta;
    }

    /// <summary>
    /// Logs POI generation results for debugging
    /// </summary>
    private void LogPOIResults()
    {
        if (_pointsOfInterest.Count == 0)
        {
            Debug.Log("=== POI Generation Complete: No Points of Interest Found ===");
            return;
        }

        Debug.Log($"=== POI Generation Complete: {_pointsOfInterest.Count} Points Found ===");

        for (int i = 0; i < _pointsOfInterest.Count; i++)
        {
            var poi = _pointsOfInterest[i];
            Debug.Log($"POI #{i + 1}: Frames {poi.startIndex}-{poi.endIndex} " +
                      $"({poi.amountOfFrames} frames) at {poi.startTimeInVideoSeconds:F2}s");
        }
    }

    /// <summary>
    /// Public accessor for generated POIs
    /// </summary>
    public List<PointOfInterest> GetPointsOfInterest()
    {
        return _pointsOfInterest;
    }

    /// <summary>
    /// Public accessor for loaded video data
    /// </summary>
    public ProcessedVideoData GetVideoData()
    {
        return _videoData;
    }
}