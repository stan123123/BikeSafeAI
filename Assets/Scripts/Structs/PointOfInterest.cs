using UnityEngine;

public struct PointOfInterest
{
    public int startIndex;
    public int endIndex;
    public float startTimeInVideoSeconds;
    public int amountOfFrames;
    public bool hasFinishedProcessingWithoutError;

    public PointOfInterest
    (
        int startIndex = 0,
        int endIndex = 0,
        float startTimeInVideoSeconds = 0f,
        int amountOfFrames = 0,
        bool hasFinishedProcessingWithoutError = true
    )
    {
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        this.startTimeInVideoSeconds = startTimeInVideoSeconds;
        this.amountOfFrames = amountOfFrames;
        this.hasFinishedProcessingWithoutError = hasFinishedProcessingWithoutError;
    }
}
