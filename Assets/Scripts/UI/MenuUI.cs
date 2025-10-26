using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button processDataButton;
    [SerializeField] private Button reviewDataButton;

    [Header("Scene Names")]
    [SerializeField] private string processDataScene;
    [SerializeField] private string reviewDataScene;

    void Start()
    {
        processDataButton.onClick.AddListener(() => LoadScene(processDataScene));
        reviewDataButton.onClick.AddListener(() => LoadScene(reviewDataScene));
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}