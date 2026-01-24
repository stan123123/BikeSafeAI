using UnityEngine;

public struct LabelFilterData
{
    public string nameReadable;
    public bool bIsIncludedInData;
    public float percentageAccuracyFilter;
    public int minAmountOfObjects;
    public int maxAmountOfObjects;

    public LabelFilterData
    (
        string nameReadable,    
        bool bIsIncludedInData,
        float percentageAccuracyFilter,
        int minAmountOfObjects,
        int maxAmountOfObjects
    )
    {
        this.nameReadable = nameReadable;
        this.bIsIncludedInData = bIsIncludedInData;
        this.percentageAccuracyFilter = percentageAccuracyFilter;
        this.minAmountOfObjects = minAmountOfObjects;
        this.maxAmountOfObjects = maxAmountOfObjects;
    }
}
