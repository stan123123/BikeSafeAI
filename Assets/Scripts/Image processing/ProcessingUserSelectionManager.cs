using UnityEngine;
using System;

public static class ProcessingUserSelectionManager
{
    public static string SelectedVideoPath { get; private set; }
    public static string SelectedVideoName { get; private set; }
    public static float SelectedDeltaSeconds { get; private set; } = -1f;

    public static void SetVideoPath(string path) => SelectedVideoPath = path;
    public static void SetVideoName(string name) => SelectedVideoName = name;
    public static void SetDeltaSeconds(float delta) => SelectedDeltaSeconds = delta;
}