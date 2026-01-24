using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class DetailsPanel : MonoBehaviour
{
    [SerializeField] private GameObject displayElementsParent;
    [SerializeField] private GameObject displayElementsPrefab;

    private ProcessedVideoData _videoData;
    private bool _videoDataLoaded;

    private List<GameObject> _activeDisplayElements = new List<GameObject>();

    private void OnEnable()
    {
        ClearElements();
        SpawnElements();

        LoadVideoData();
    }

    private void OnDisable()
    {

    }

    private bool LoadVideoData()
    {
        string videoDataPath = Path.Combine(ReviewUserSelectionManager.SelectedPath, "ProcessedVideoData.json");

        if (!File.Exists(videoDataPath))
        {
            Debug.LogError($"ProcessedVideoData.json not found at: {videoDataPath}");
            return false;
        }

        try
        {
            string jsonContent = File.ReadAllText(videoDataPath);
            _videoData = JsonUtility.FromJson<ProcessedVideoData>(jsonContent);
            _videoDataLoaded = true;

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse ProcessedVideoData.json: {e.Message}");
            return false;
        }
    }

    private void SpawnElements()
    {
        List<LabelFilterData> filterData = ReviewUserSelectionManager.LabelFilters;

        for (int i = 0; i < filterData.Count; i++)
        {
            GameObject element = Instantiate(
                displayElementsPrefab,
                displayElementsParent.transform
            );

            _activeDisplayElements.Add(element);

            DetailsDisplayElement displayElement =
                element.GetComponent<DetailsDisplayElement>();

            if (displayElement != null)
            {
                displayElement.SetName(filterData[i].nameReadable);
            }
        }
    }

    private void ClearElements()
    {
        for (int i = 0; i < _activeDisplayElements.Count; i++)
        {
            Destroy(_activeDisplayElements[i]);
        }

        _activeDisplayElements.Clear();
    }

    public void UpdatePreviewDetails(float videoTimeSeconds)
    {
        if (!_videoDataLoaded)
            return;

        List<LabelFilterData> filterData = ReviewUserSelectionManager.LabelFilters;

        // Load the config once (legend)
        ConfigData config = ConfigLoader.LoadDefaultAnnotationConfig();
        if (config == null || config.labels == null)
            return;

        // Build a quick lookup: internal name -> readable name
        var internalToReadable = config.labels.ToDictionary(l => l.name, l => l.readable);

        // Calculate frame number
        int frameIndex = Mathf.FloorToInt(videoTimeSeconds / _videoData.derivedFrameDeltaSeconds);
        int frameNumber = frameIndex + 1;

        // Build path to frame JSON
        string basePath = ReviewUserSelectionManager.SelectedPath;
        string jsonFolder = Path.Combine(basePath, PathConfig.ImageStatsJsonFolderName);
        string fileName = $"frame_{frameNumber:D5}_analysis.json";
        string fullPath = Path.Combine(jsonFolder, fileName);

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"Frame JSON not found: {fullPath}");
            return;
        }

        // Load ImageAnnotationData (contains actual label stats)
        string jsonContent = File.ReadAllText(fullPath);
        ImageAnnotationData frameData = JsonUtility.FromJson<ImageAnnotationData>(jsonContent);

        if (frameData == null || frameData.labels == null)
            return;

        Debug.Log($"--- Frame at {videoTimeSeconds}s ---");

        // Loop over all display elements and update counts
        for (int i = 0; i < _activeDisplayElements.Count; i++)
        {
            GameObject element = _activeDisplayElements[i];
            DetailsDisplayElement displayElement = element.GetComponent<DetailsDisplayElement>();

            if (displayElement == null)
                continue;

            string elementName = displayElement.GetName(); // e.g., "Car", "Bicycle"

            // Find the LabelStats whose readable name matches this UI element
            LabelStats stats = frameData.labels
                .FirstOrDefault(ls =>
                    internalToReadable.TryGetValue(ls.labelName, out string readable) &&
                    readable == elementName);

            int count = stats != null ? stats.instanceCount : 0;

            displayElement.SetCount(count);

            Debug.Log($"Element '{elementName}' -> Count: {count}");
        }
    }

    private ConfigData LoadCurrentFrameDataFromTime(float timeSeconds)
    {
        int frameIndex = Mathf.FloorToInt(
            timeSeconds / _videoData.derivedFrameDeltaSeconds
        );

        int frameNumber = frameIndex + 1;

        string basePath = ReviewUserSelectionManager.SelectedPath;
        string jsonFolder = Path.Combine(
            basePath,
            PathConfig.ImageStatsJsonFolderName
        );

        string fileName = $"frame_{frameNumber:D5}_analysis.json";
        string fullPath = Path.Combine(jsonFolder, fileName);

        return ConfigLoader.LoadConfigFromJsonFile(fullPath);
    }
}
