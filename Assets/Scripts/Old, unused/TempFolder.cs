using UnityEngine;
using System.IO;

// this stuff is used to quickly make a temp folder so we can store images we want to process.
public static class TempFolder
{
    public static string GetTempPath()
    {
        // This works in Editor & Build
        string tempPath = Path.Combine(Application.persistentDataPath, "TempImages");

        if (!Directory.Exists(tempPath))
            Directory.CreateDirectory(tempPath);

        return tempPath;
    }
}