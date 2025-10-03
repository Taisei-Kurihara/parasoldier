using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

// 動的オブジェクトコントローラーのインターフェース定義
// シーンの読み込み・解放時の処理を実装するためのインターフェース
public interface DynamicObjectController_interface
{
    /// <summary> シーンのロードが完了した後に呼び出されるメソッド </summary>
    public UniTask OnSceneLoadedAsync();

    /// <summary> シーンのアンロードが完了した後に呼び出されるメソッド </summary>
    public UniTask OnSceneUnloadedAsync();
}