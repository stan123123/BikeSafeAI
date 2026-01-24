using UnityEngine;
using UnityEngine.UI;

public class BaseUIPanelChangeButton : MonoBehaviour
{
    [SerializeField] private string PanelIndexToSwitchTo = "";

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("BaseUIPanelChangeButton requires a Button component on the same GameObject.");
            return;
        }
    }

    void OnEnable()
    {
        button.onClick.AddListener(OnButtonPressed);
    }

    void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(OnButtonPressed);
    }

    private void OnButtonPressed()
    {
        UIManager.RequestUIChange(PanelIndexToSwitchTo.ToString());
        Debug.Log($"Button pressed, requested UI change to panel index {PanelIndexToSwitchTo}");
    }
}