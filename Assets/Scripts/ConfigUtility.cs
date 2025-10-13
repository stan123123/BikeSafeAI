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
    public static ImageAnnotationData AnalyzeImage(Texture2D coloredTexture, LabelData[] legend, Texture2D panopticTexture = null)
    {
        Color[] colorPixels = coloredTexture.GetPixels();
        int totalPixels = colorPixels.Length;

        var result = new ImageAnnotationData { imageName = coloredTexture.name };
        var labelStats = new Dictionary<string, LabelStats>();

        foreach (var label in legend)
            labelStats[label.name] = new LabelStats { labelName = label.name };

        // --- AREA CALCULATION ---
        for (int i = 0; i < colorPixels.Length; i++)
        {
            var pixel = colorPixels[i];
            foreach (var label in legend)
            {
                if (ColorsMatch(pixel, label.color))
                {
                    labelStats[label.name].pixelCount++;
                    break;
                }
            }
        }

        foreach (var label in legend)
        {
            var stats = labelStats[label.name];
            stats.areaPercent = (float)stats.pixelCount / totalPixels * 100f;
        }

        // --- INSTANCE COUNT (panoptic comparison) ---
        if (panopticTexture != null)
        {
            Color[] panopticPixels = panopticTexture.GetPixels();

            foreach (var label in legend)
            {
                HashSet<int> uniqueInstanceColors = new HashSet<int>();

                for (int i = 0; i < colorPixels.Length; i++)
                {
                    // only check where this label exists in the colored mask
                    if (!ColorsMatch(colorPixels[i], label.color))
                        continue;

                    Color p = panopticPixels[i];

                    // Convert to 0–255 integer color (avoids float drift)
                    int r = Mathf.RoundToInt(p.r * 255f);
                    int g = Mathf.RoundToInt(p.g * 255f);
                    int b = Mathf.RoundToInt(p.b * 255f);

                    // combine into single int for hash comparison
                    int colorKey = (r << 16) | (g << 8) | b;
                    uniqueInstanceColors.Add(colorKey);
                }

                labelStats[label.name].instanceCount = uniqueInstanceColors.Count;
            }
        }

        result.labels.AddRange(labelStats.Values);
        return result;
    }

    private static bool ColorsMatch(Color c, int[] rgb, float tolerance = 0.01f)
    {
        return Mathf.Abs(c.r * 255 - rgb[0]) < tolerance * 255 &&
               Mathf.Abs(c.g * 255 - rgb[1]) < tolerance * 255 &&
               Mathf.Abs(c.b * 255 - rgb[2]) < tolerance * 255;
    }

    // Proper comparer for Color in HashSet
    private class ColorComparer : IEqualityComparer<Color>
    {
        public bool Equals(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.001f &&
                   Mathf.Abs(a.g - b.g) < 0.001f &&
                   Mathf.Abs(a.b - b.b) < 0.001f;
        }

        public int GetHashCode(Color c)
        {
            int r = Mathf.RoundToInt(c.r * 255);
            int g = Mathf.RoundToInt(c.g * 255);
            int b = Mathf.RoundToInt(c.b * 255);
            return (r << 16) | (g << 8) | b;
        }
    }

    public static void SaveToJson(ImageAnnotationData data, string packageName)
    {
        string outputDir = PathConfig.GetPackagedImageStatsFolder(packageName);

        // Sanitize filename and remove ALL extensions (handles cases like .json, .png, etc.)
        string baseName = data.imageName;

        // Strip all extensions until we have just the base name
        while (Path.HasExtension(baseName))
        {
            baseName = Path.GetFileNameWithoutExtension(baseName);
        }

        // Sanitize path separators
        string safeName = baseName
            .Replace("/", "_")
            .Replace("\\", "_");

        string outputPath = Path.Combine(outputDir, $"{safeName}.json");

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(outputPath, json);

        Debug.Log($"Saved image statistics JSON: {outputPath}");
    }
}