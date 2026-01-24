using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Static class to act as a single source of truth when it comes to the user's selections in filters and more in the review scene
/// </summary>
public static class ReviewUserSelectionManager
{
    public static string SelectedPath { get; private set; }
    public static bool bSelectedWholeVideoArea { get; private set; } = true;
    public static Vector2Int BottomLeft { get; private set; }
    public static Vector2Int BottomRight { get; private set; }
    public static float MinimumDesiredAccuracy { get; private set; } = 75f;
    public static List<string> IncludedLabels { get; private set; } = new List<string>(); // important: includes nameReadable, not .name!!!
    public static List<LabelFilterData> LabelFilters { get; private set; } = new List<LabelFilterData>();

    public static void SetPath(string path) => SelectedPath = path;
    public static void SetVideoName(bool bIsTrue) => bSelectedWholeVideoArea = bIsTrue;
    public static void SetBottomLeft(Vector2Int location) => BottomLeft = location;
    public static void SetBottomRight(Vector2Int location) => BottomRight = location;
    public static void SetMinimumDesiredAccuracy(float accuracy) => MinimumDesiredAccuracy = accuracy;
    public static void SetIncludedLabels(List<string> labels) => IncludedLabels = labels;
    public static void SetLabelFilters(List<LabelFilterData> filterData) => LabelFilters = filterData;

    /// <summary>
    /// Adds a label if it doesn't already exist in the list.
    /// </summary>
    public static void AddLabel(string label)
    {
        if (!IncludedLabels.Contains(label))
        {
            IncludedLabels.Add(label);
        }
    }

    /// <summary>
    /// Removes a label if it exists in the list.
    /// </summary>
    public static void RemoveLabel(string label)
    {
        if (IncludedLabels.Contains(label))
        {
            IncludedLabels.Remove(label);
        }
    }

    /// <summary>
    /// Overwrites a lablel if it already exists, otherwise adds it to the list
    /// </summary>
    /// <param name="filter"></param>
    public static void AddLabelFilter(LabelFilterData filter)
    {
        for (int i = 0; i < LabelFilters.Count; i++)
        {
            if (LabelFilters[i].nameReadable == filter.nameReadable)
            {
                LabelFilters[i] = filter;
                return;
            }
        }

        LabelFilters.Add(filter);
    }

    /// <summary>
    /// Removes label filter based on readable name
    /// </summary>
    /// <param name="filter"></param>
    public static void RemoveLabelFilter(LabelFilterData filter)
    {
        for (int i = LabelFilters.Count - 1; i >= 0; i--)
        {
            if (LabelFilters[i].nameReadable == filter.nameReadable)
            {
                LabelFilters.RemoveAt(i);
                return;
            }
        }
    }
}