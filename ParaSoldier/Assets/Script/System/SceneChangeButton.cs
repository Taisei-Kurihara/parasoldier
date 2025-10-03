using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// シーン変更ボタンクラス
/// ボタンクリック時に指定されたシーンへの遷移を行う
/// </summary>
public class SceneChangeButton : MonoBehaviour
{
    // 遷移先のシーン
    [SerializeField]
    UseScene useScene = UseScene.Game;

    /// <summary>
    /// 初期化処理
    /// ボタンコンポーネントにクリックイベントを登録する
    /// </summary>
    private void Awake()
    {
        var button = GetComponent<UnityEngine.UI.Button>();

        button.onClick.AddListener(Onclick);
    }

    /// <summary>
    /// ボタンクリック時の処理
    /// 指定されたシーンへの遷移を実行する
    /// </summary>
    void Onclick()
    {
        SceneManagerSingleton.Instance().ChangeScene(useScene).Forget();
    }
}
