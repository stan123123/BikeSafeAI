using System;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a bounding box for an instance
/// </summary>
[Serializable]
public struct BoundingBox
{
    public int minX;
    public int minY;
    public int maxX;
    public int maxY;

    public BoundingBox(int minX, int minY, int maxX, int maxY)
    {
        this.minX = minX;
        this.minY = minY;
        this.maxX = maxX;
        this.maxY = maxY;
    }

    public int Width => maxX - minX;
    public int Height => maxY - minY;
}

/// <summary>
/// Represents the center position of an instance
/// </summary>
[Serializable]
public struct InstancePosition
{
    public int x;
    public int y;

    public InstancePosition(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

/// <summary>
/// Represents a single detected instance from the panoptic segmentation
/// </summary>
[Serializable]
public class DetectedInstance
{
    public int id;
    public int label_id;
    public bool was_fused;
    public float score;
}

/// <summary>
/// Represents detailed information about a single instance including its location
/// </summary>
[Serializable]
public class InstanceInfo
{
    public int id;
    public float score;
    public BoundingBox bbox;
    public InstancePosition center;
}

/// <summary>
/// Represents statistics for a single label within one annotated image.
/// </summary>
[Serializable]
public class LabelStats
{
    public string labelName;
    public int labelId;
    public int instanceCount;
    public float averageScore;
    public InstanceInfo[] instances = new InstanceInfo[0];
}

/// <summary>
/// Represents the full analysis results for one annotated image.
/// </summary>
[Serializable]
public class ImageAnnotationData
{
    public string imageName;
    public int totalInstances;
    public List<LabelStats> labels = new List<LabelStats>();
}

/// <summary>
/// Represents a single label entry in the JSON.
/// </summary>
[Serializable]
public class LabelData
{
    public string name;
    public string readable;
    public bool instances;
    public bool evaluate;
    public int[] color; // RGB as [R, G, B]
}

/// <summary>
/// Root structure of the config JSON.
/// </summary>
[Serializable]
public class ConfigData
{
    public List<LabelData> labels;
    public float version;
    public string mapping;
    public string folder_structure;
}

/// <summary>
/// Utility class to load JSON into ConfigData
/// </summary>
public static class ConfigLoader
{
    /// <summary>
    /// Loads a JSON file from disk and deserializes it into a ConfigData object
    /// </summary>
    public static ConfigData LoadConfigFromJsonFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            UnityEngine.Debug.LogError($"Config file not found: {filePath}");
            return null;
        }

        string jsonText = File.ReadAllText(filePath);
        return JsonUtility.FromJson<ConfigData>(jsonText);
    }

    /// <summary>
    /// Automatically loads the default annotation config from PathConfig
    /// </summary>
    public static ConfigData LoadDefaultAnnotationConfig()
    {
        return LoadConfigFromJsonFile(PathConfig.AnnotationConfigFile);
    }

    public static LabelData[] GetLabels(ConfigData config)
    {
        return config?.labels?.ToArray();
    }

    /// <summary>
    /// Gets a label by its ID from the config
    /// </summary>
    public static LabelData GetLabelById(ConfigData config, int labelId)
    {
        if (config?.labels == null) return null;

        // Assuming label_id corresponds to the index in the labels array
        if (labelId >= 0 && labelId < config.labels.Count)
        {
            return config.labels[labelId];
        }

        return null;
    }
}

public static class AnnotationAnalyzer
{
    /// <summary>
    /// Analyzes an image using the panoptic objects JSON and panoptic instance mask
    /// to extract bounding boxes like the Python script does
    /// </summary>
    public static ImageAnnotationData AnalyzeImage(LabelData[] legend, string panopticObjectsJsonPath, string panopticInstanceMaskPath, string imageName = null)
    {
        var result = new ImageAnnotationData
        {
            imageName = imageName ?? Path.GetFileNameWithoutExtension(panopticObjectsJsonPath)
        };

        if (!File.Exists(panopticObjectsJsonPath))
        {
            UnityEngine.Debug.LogError($"Panoptic objects JSON not found: {panopticObjectsJsonPath}");
            return result;
        }

        // Read and parse the JSON file
        string jsonText = File.ReadAllText(panopticObjectsJsonPath);

        DetectedInstance[] instances;
        try
        {
            // Try to parse as array directly
            instances = JsonHelper.FromJson<DetectedInstance>(jsonText);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to parse panoptic objects JSON: {panopticObjectsJsonPath}\n{e}");
            return result;
        }

        if (instances == null || instances.Length == 0)
        {
            UnityEngine.Debug.LogWarning($"No instances found in {panopticObjectsJsonPath}");
            return result;
        }

        result.totalInstances = instances.Length;

        // DEBUG: Log instance IDs from JSON
        UnityEngine.Debug.Log($"Found {instances.Length} instances in JSON with IDs: {string.Join(", ", instances.Select(i => i.id))}");

        // Load the panoptic instance mask to extract bounding boxes
        Dictionary<int, BoundingBox> bboxMap = new Dictionary<int, BoundingBox>();
        if (File.Exists(panopticInstanceMaskPath))
        {
            bboxMap = ExtractBoundingBoxesFromMask(panopticInstanceMaskPath);

            // DEBUG: Log instance IDs from mask
            UnityEngine.Debug.Log($"Extracted {bboxMap.Count} bounding boxes with IDs: {string.Join(", ", bboxMap.Keys)}");

            // DEBUG: Check for mismatches
            var jsonIds = new HashSet<int>(instances.Select(i => i.id));
            var maskIds = new HashSet<int>(bboxMap.Keys);
            var missingInMask = jsonIds.Except(maskIds).ToList();
            var extraInMask = maskIds.Except(jsonIds).ToList();

            if (missingInMask.Count > 0)
            {
                UnityEngine.Debug.LogWarning($"Instance IDs in JSON but NOT in mask: {string.Join(", ", missingInMask)}");
            }
            if (extraInMask.Count > 0)
            {
                UnityEngine.Debug.LogWarning($"Instance IDs in mask but NOT in JSON: {string.Join(", ", extraInMask)}");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning($"Panoptic instance mask not found: {panopticInstanceMaskPath}. Bounding boxes will not be calculated.");
        }

        // Create a config for label lookup
        var config = new ConfigData { labels = legend.ToList() };

        // Group instances by label_id
        var groupedByLabel = instances.GroupBy(inst => inst.label_id);

        foreach (var group in groupedByLabel)
        {
            int labelId = group.Key;
            var instancesList = group.ToList();

            // Get label info from legend
            LabelData labelInfo = ConfigLoader.GetLabelById(config, labelId);
            string labelName = labelInfo != null ? labelInfo.name : $"Unknown_Label_{labelId}";

            var instanceInfos = new List<InstanceInfo>();
            foreach (var inst in instancesList)
            {
                BoundingBox bbox = bboxMap.ContainsKey(inst.id) ? bboxMap[inst.id] : new BoundingBox(0, 0, 0, 0);

                // DEBUG: Log when bbox is not found
                if (!bboxMap.ContainsKey(inst.id))
                {
                    UnityEngine.Debug.LogWarning($"Instance ID {inst.id} (label: {labelName}) not found in bboxMap!");
                }

                InstancePosition center = new InstancePosition(
                    (bbox.minX + bbox.maxX) / 2,
                    (bbox.minY + bbox.maxY) / 2
                );

                instanceInfos.Add(new InstanceInfo
                {
                    id = inst.id,
                    score = inst.score,
                    bbox = bbox,
                    center = center
                });
            }

            var labelStat = new LabelStats
            {
                labelName = labelName,
                labelId = labelId,
                instanceCount = instancesList.Count,
                averageScore = instancesList.Average(i => i.score),
                instances = instanceInfos.ToArray()
            };

            result.labels.Add(labelStat);
        }

        return result;
    }

    private static Dictionary<int, BoundingBox> ExtractBoundingBoxesFromMask(string instanceMaskPath)
    {
        var bboxMap = new Dictionary<int, BoundingBox>();

        try
        {
            byte[] maskBytes = File.ReadAllBytes(instanceMaskPath);
            Texture2D maskTex = new Texture2D(2, 2);

            // Important: LoadImage can flip the texture depending on format
            // We want to load it without any automatic processing
            if (!maskTex.LoadImage(maskBytes))
            {
                UnityEngine.Debug.LogError($"Failed to load texture from: {instanceMaskPath}");
                return bboxMap;
            }

            Color32[] pixels = maskTex.GetPixels32();
            int width = maskTex.width;
            int height = maskTex.height;

            UnityEngine.Debug.Log($"Mask dimensions: {width}x{height}, Total pixels: {pixels.Length}");

            var instancePositions = new Dictionary<int, (int minX, int minY, int maxX, int maxY)>();

            // Sample first few pixels to see what we're getting
            UnityEngine.Debug.Log($"First 5 pixels (raw): {string.Join(", ", pixels.Take(5).Select(p => $"R:{p.r} G:{p.g} B:{p.b}"))}");

            // Unity's GetPixels32() returns pixels from bottom-left, going right then up
            // We need to flip the Y coordinate to match the standard image coordinate system
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate the correct pixel index
                    // Unity uses bottom-left origin, so we need to flip Y
                    int pixelIndex = (height - 1 - y) * width + x;
                    Color32 pixel = pixels[pixelIndex];

                    // Decode instance ID from RGB (matching Python encoding)
                    int instanceId = pixel.r + (pixel.g << 8) + (pixel.b << 16);

                    if (instanceId == 0) continue; // skip background

                    if (!instancePositions.ContainsKey(instanceId))
                    {
                        instancePositions[instanceId] = (x, y, x, y);
                    }
                    else
                    {
                        var cur = instancePositions[instanceId];
                        instancePositions[instanceId] = (
                            Math.Min(cur.minX, x),
                            Math.Min(cur.minY, y),
                            Math.Max(cur.maxX, x),
                            Math.Max(cur.maxY, y)
                        );
                    }
                }
            }

            UnityEngine.Debug.Log($"Found {instancePositions.Count} unique instance IDs in mask");

            // Sample some instance IDs and their bboxes
            var sampleIds = instancePositions.Keys.Take(5).ToList();
            UnityEngine.Debug.Log($"Sample instance IDs found: {string.Join(", ", sampleIds)}");

            foreach (var kvp in instancePositions)
            {
                bboxMap[kvp.Key] = new BoundingBox(kvp.Value.minX, kvp.Value.minY, kvp.Value.maxX, kvp.Value.maxY);

                // Log first few bboxes
                if (kvp.Key <= 5)
                {
                    UnityEngine.Debug.Log($"Instance {kvp.Key} bbox: ({kvp.Value.minX}, {kvp.Value.minY}) to ({kvp.Value.maxX}, {kvp.Value.maxY})");
                }
            }

            UnityEngine.Debug.Log($"Extracted {bboxMap.Count} bounding boxes from: {instanceMaskPath}");
            UnityEngine.Object.Destroy(maskTex);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to extract bounding boxes: {e}");
        }

        return bboxMap;
    }

    /// <summary>
    /// Saves the analysis data to a JSON file in the package's imageStatsJson folder
    /// </summary>
    public static void SaveToJson(ImageAnnotationData data, string packageName)
    {
        // Save JSON to imageStatsJson folder
        string outputFolder = PathConfig.GetPackagedImageStatsJsonFolder(packageName);

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        string outputPath = Path.Combine(outputFolder, $"{data.imageName}_analysis.json");
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(outputPath, json);
        UnityEngine.Debug.Log($"Saved image analysis JSON: {outputPath}");
    }
}

/// <summary>
/// Helper class for JSON array serialization (Unity's JsonUtility doesn't handle arrays well)
/// </summary>
static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{ \"items\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.items;
    }

    public static string ToJson<T>(T[] array, bool prettyPrint = false)
    {
        Wrapper<T> wrapper = new Wrapper<T> { items = array };
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] items;
    }
}