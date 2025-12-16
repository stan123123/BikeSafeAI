using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using SFB;
using System.IO;

/// <summary>
/// This static class basically just checks if the folder name we try to use is already used inside of the output folder
/// </summary>
public static class ProcessedFolderNameValidator
{
    public static bool IsFolderNameAvailable(string folderName)
    {
        string path = Path.Combine(PathConfig.OutputFolder, "packagedData", folderName);
        return !Directory.Exists(path);
    }
}
