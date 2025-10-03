using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;

/// <summary>
/// 動的オブジェクト管理クラス
/// Addressableアセットの読み込みとシーン切り替えに応じたコントローラーの管理を行う
/// </summary>
public class DynamicObjectManager : Singleton_DestroyAvailableMonoSingleton<DynamicObjectManager>
{
    // 使用する動的オブジェクトコントローラー
    DynamicObjectController_interface UseDO = new DynamicObjectController_Default();

    // ロードされたアセットを保持する変数
    private Object loadedSceneAsset;

    /// <summary>
    /// シーンの設定を行う
    /// 指定されたシーンに応じてコントローラーを切り替え、必要な初期化処理を実行する
    /// </summary>
    /// <param name="scene">設定するシーン</param>
    public async UniTask SetScene(UseScene scene)
    {
        // 既存のアセットが存在する場合は解放
        if (loadedSceneAsset != null)
        {
            ReleaseAsset(loadedSceneAsset);
            loadedSceneAsset = null;
        }

        // デフォルトで初期化
        UseDO = new DynamicObjectController_Default();

        // シーンに応じてコントローラー切り替え
        switch (scene)
        {
            case UseScene.Game:
                UseDO = new DynamicObjectController_Game();
                break;
            default:
                break;
        }

        // シーンロード後の処理を実行
        await UseDO.OnSceneLoadedAsync();
    }

    /// <summary>
    /// Addressableアセットを非同期で読み込む
    /// </summary>
    /// <typeparam name="T">読み込むオブジェクトの型</typeparam>
    /// <param name="address">アセットのアドレス</param>
    /// <returns>読み込まれたアセット</returns>
    public async UniTask<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
    {
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
        await handle.ToUniTask();
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            loadedSceneAsset = handle.Result;
            return handle.Result;
        }
        else
        {
            Debug.LogError($"Addressable Load Failed: {address}");
        }
        return null;
    }

    /// <summary>
    /// アセットを解放する
    /// </summary>
    /// <param name="asset">解放するアセット</param>
    private void ReleaseAsset(Object asset)
    {
        Addressables.Release(asset);
    }
}
