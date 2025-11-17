using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ProcessingProgressUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressSlider;

    private void OnEnable()
    {
        // Subscribe to progress events
        ImageProcesser.OnProcessingProgress += UpdateProgressUI;

        // Get current state in case we enable mid-process
        ImageProcesser.GetCurrentProgress(out int current, out int total);
        UpdateProgressUI(current, total);
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid leaks
        ImageProcesser.OnProcessingProgress -= UpdateProgressUI;
    }

    private void UpdateProgressUI(int current, int total)
    {
        int displayCurrent = Mathf.Max(current - 1, 0);

        if (progressText != null)
        {
            progressText.text = $"{displayCurrent}/{total} Frames";
        }

        if (progressSlider != null)
        {
            if (total > 0)
                progressSlider.value = (float)displayCurrent / total;
            else
                progressSlider.value = 0f;
        }
    }
}