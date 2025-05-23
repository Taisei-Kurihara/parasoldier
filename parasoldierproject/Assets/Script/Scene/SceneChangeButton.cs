using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class SceneChangeButton : MonoBehaviour
{
    [SerializeField]
    private SceneName sceneName;

    private void Awake()
    {
        GetComponent<Button>().OnClickAsObservable()
            .Subscribe(button =>  SceneLoader.Instance.LoadNextScene(sceneName.ToString() ))
            .AddTo(this);
    }
}