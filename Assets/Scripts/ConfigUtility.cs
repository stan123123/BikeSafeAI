using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents statistics for a single label within one annotated image.
/// </summary>
[Serializable]
public class LabelStats
{
    public string labelName;
    public int instanceCount;
    public int pixelCount;
    public float areaPercent;
}

/// <summary>
/// Represents the full analysis results for one annotated image.
/// </summary>
[Serializable]
public class ImageAnnotationData
{
    public string imageName;
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
            Debug.LogError($"Config file not found: {filePath}");
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
}


public static class AnnotationAnalyzer
{
    /// <summary>
    /// Analyzes an annotated Texture2D using legend data and returns statistics.
    /// </summary>
    public static ImageAnnotationData AnalyzeImage(Texture2D annotatedTexture, LabelData[] legend)
    {
        Color[] pixels = annotatedTexture.GetPixels();
        int totalPixels = pixels.Length;

        var result = new ImageAnnotationData
        {
            imageName = annotatedTexture.name
        };

        // Prepare stats containers
        var labelStats = new Dictionary<string, LabelStats>();
        foreach (var label in legend)
        {
            labelStats[label.name] = new LabelStats { labelName = label.name };
        }

        // Count pixels per label
        foreach (var pixel in pixels)
        {
            foreach (var label in legend)
            {
                if (ColorsMatch(pixel, label.color))
                {
                    labelStats[label.name].pixelCount++;
                    break;
                }
            }
        }

        // Convert to percentage
        foreach (var stats in labelStats.Values)
        {
            if (stats.pixelCount > 0)
            {
                stats.areaPercent = (float)stats.pixelCount / totalPixels * 100f;
                result.labels.Add(stats);
            }
        }

        // TODO: add instanceCount later using connected component analysis
        return result;
    }

    /// <summary>
    /// Compares Unity Color to RGB array with small tolerance
    /// </summary>
    private static bool ColorsMatch(Color c, int[] rgb, float tolerance = 0.01f)
    {
        return Mathf.Abs(c.r * 255 - rgb[0]) < tolerance * 255 &&
               Mathf.Abs(c.g * 255 - rgb[1]) < tolerance * 255 &&
               Mathf.Abs(c.b * 255 - rgb[2]) < tolerance * 255;
    }

    private static bool ColorsMatch(Color c, int[] rgb) => ColorsMatch(c, rgb, 0.01f);

    /// <summary>
    /// Saves image annotation data as JSON
    /// </summary>
    public static void SaveToJson(ImageAnnotationData data, string outputPath)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(outputPath, json);
    }
}