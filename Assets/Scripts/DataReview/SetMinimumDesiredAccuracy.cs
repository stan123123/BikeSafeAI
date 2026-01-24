using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetMinimumDesiredAccuracy : MonoBehaviour
{
    [SerializeField] private Slider desiredAccuracySlider;
    [SerializeField] private TMP_Text desiredAccuracyPercentage;

    private void OnEnable()
    {
        desiredAccuracySlider.onValueChanged.AddListener(ChangeDisplayedDesiredAccuracyPercentage);

        ChangeDisplayedDesiredAccuracyPercentage(desiredAccuracySlider.value);
    }

    private void OnDisable()
    {
        desiredAccuracySlider.onValueChanged.RemoveListener(ChangeDisplayedDesiredAccuracyPercentage);
    }

    private void ChangeDisplayedDesiredAccuracyPercentage(float newFactor)
    {
        float newPercentage = newFactor * 100;

        desiredAccuracyPercentage.text = ((int)newPercentage).ToString() + "%";

        ReviewUserSelectionManager.SetMinimumDesiredAccuracy(newPercentage);
    }
}
