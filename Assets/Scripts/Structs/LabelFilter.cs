using UnityEngine;

public struct LabelFilter
{
    public string labelNameReadable;
    public int minimumObjects;
    public int maximumObjects;
    public int desiredAccuracy;

    public LabelFilter
    (
        string labelNameReadable = "",
        int minimumObjects = 0,
        int maximumObjects = 0,
        int desiredAccuracy = 0
    )
    {
        this.labelNameReadable = labelNameReadable;
        this.minimumObjects = minimumObjects;
        this.maximumObjects = maximumObjects;
        this.desiredAccuracy = desiredAccuracy;
    }
}
