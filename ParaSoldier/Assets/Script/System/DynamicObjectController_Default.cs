using Cysharp.Threading.Tasks;
using UnityEngine;

// デフォルトの動的オブジェクトコントローラー実装
// 特別な処理が不要な場合に使用する基本実装
public class DynamicObjectController_Default : DynamicObjectController_interface
{
    // シーンロード完了時の処理（デフォルトは空実装）
    public async UniTask OnSceneLoadedAsync()
    {
        // 必要に応じてオーバーライドして実装
    }

    // シーンアンロード完了時の処理（デフォルトは空実装）
    public async UniTask OnSceneUnloadedAsync()
    {
        // 必要に応じてオーバーライドして実装
    }
}
