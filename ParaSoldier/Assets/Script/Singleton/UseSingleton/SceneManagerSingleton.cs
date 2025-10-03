using System.Diagnostics;
using Common;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using R3;
using R3.Collections;
using UnityEngine;

/// <summary>
/// 使用可能なシーンを定義する列挙型
/// </summary>
public enum UseScene
{
    Title,
    Game
}

/// <summary>
/// シーン管理シングルトンクラス
/// シーンの切り替えと現在のシーン状態を管理する
/// </summary>
public class SceneManagerSingleton : Singleton_MonoBehaviourBase<SceneManagerSingleton>
{

    // 現在のシーン
    public UseScene CurrentScene { get; private set; }

    // シーンローディング中かどうかを示すフラグ
    private bool isLoading = false;

    /// <summary>
    /// 指定されたシーンに変更する
    /// 既にローディング中または同じシーンの場合は処理をスキップする
    /// </summary>
    /// <param name="newScene">変更先のシーン</param>
    public async UniTask ChangeScene(UseScene newScene)
    {
        var token = this.GetCancellationTokenOnDestroy();
        if (isLoading || CurrentScene == newScene) return;

        isLoading = true;
        await LoadSceneAsync(newScene);
        CurrentScene = newScene;
        isLoading = false;
    }

    /// <summary>
    /// シーンを非同期でロードする
    /// フェードイン・アウト効果と共にシーンを切り替える
    /// </summary>
    /// <param name="scene">ロードするシーン</param>
    async UniTask LoadSceneAsync(UseScene scene)
    {
        // フェード用キャンバスを呼び出し
        var fadeHandle = Addressables.InstantiateAsync("Load");
        await fadeHandle.Task;
        var fadeObj = fadeHandle.Result;
        fadeObj.transform.SetParent(transform);
        
        // UIManagerのCanvas親子付け関数で親子付け
        await UImanager.Instance().AttachToCanvas(fadeObj);

        var loadScene = fadeObj.GetComponent<LoadScene_interface>();
        if (loadScene != null)
        {
            await loadScene.StartFadeIn();
        }
        else
        {
            UnityEngine.Debug.LogWarning("LoadScene_interfaceがアタッチされていません");
        }

        // シーンをロード（非常に重要）
        UnityEngine.Debug.Log("LoadSceneAsync");
        var sceneHandle = Addressables.LoadSceneAsync(scene.ToString(), LoadSceneMode.Single);
        UnityEngine.Debug.Log("sceneHandle");
        await sceneHandle.Task;

        UnityEngine.Debug.Log("DynamicObjectManager");
        // DynamicObjectManagerに通知
        await DynamicObjectManager.Instance().SetScene(scene);

        if (loadScene != null)
        {
            UnityEngine.Debug.Log("StartFadeOut");
            await loadScene.StartFadeOut();
        }
    }
}