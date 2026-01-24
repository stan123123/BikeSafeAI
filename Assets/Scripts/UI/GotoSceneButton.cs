using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GotoSceneButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button GoToSceneButton;

    [Header("Scene Name")]
    [SerializeField] private string SceneToGoTo;

    void Start()
    {
        GoToSceneButton.onClick.AddListener(() => LoadScene(SceneToGoTo));
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
