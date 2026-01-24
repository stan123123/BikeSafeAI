using UnityEngine;
using System;

public struct ProcessedVideoData
{
    public bool hasGPSData;
    public float totalFrames;
    public float videoDurationSeconds;
    public float derivedFrameDeltaSeconds => videoDurationSeconds / totalFrames;
    public float userInputFrameDeltaSeconds;
    public bool hasFinishedProcessingWithoutError;

    public ProcessedVideoData
    (
        bool hasGPSData = false,
        float totalFrames = 1,
        float videoDurationSeconds = 0f,
        float userInputFrameDeltaSeconds = 0f,
        bool hasFinishedProcessingWithoutError = true
    )
    {
        this.hasGPSData = hasGPSData;
        this.totalFrames = totalFrames;
        this.videoDurationSeconds = videoDurationSeconds;   
        this.userInputFrameDeltaSeconds = userInputFrameDeltaSeconds;
        this.hasFinishedProcessingWithoutError = hasFinishedProcessingWithoutError;
    }
}
