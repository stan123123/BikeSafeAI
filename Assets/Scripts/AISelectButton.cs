using UnityEngine;
using UnityEngine.UI;

public class AISelectionButton : MonoBehaviour
{
    [Header("Assign the Button in Inspector")]
    [SerializeField] private Button aiButton;

    private void OnEnable()
    {
        if (aiButton != null)
            aiButton.onClick.AddListener(OnButtonClicked);
    }

    private void OnDisable()
    {
        if (aiButton != null)
            aiButton.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        // Broadcast UI change to ProcessingImages
        UIManager.RequestUIChange(UIManager.UIType.SelectFile);

        // TODO: later you can also set the processing type in ImageProcesser
        // Example: ImageProcesser.SetProcessingType(ImageProcesser.ProcessingType.Annotation);
    }
}
