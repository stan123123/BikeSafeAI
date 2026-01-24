using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BaseBackButton : MonoBehaviour
{
    private Button _backButton;

    private void Awake()
    {
        _backButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_backButton != null)
        {
            _backButton.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnDisable()
    {
        if (_backButton != null)
        {
            _backButton.onClick.RemoveListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        UIManager.RequestBack();

        Debug.Log("Requesting to go back");
    }
}