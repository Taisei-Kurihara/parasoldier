using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneChangeButton : MonoBehaviour
{
    [SerializeField]
    SceneName sceneName;

    private void Awake()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(SceneLoad);
    }

    private void SceneLoad()
    {
        SceneLoader.Instance.LoadNextScene(sceneName.ToString());
    }
}
