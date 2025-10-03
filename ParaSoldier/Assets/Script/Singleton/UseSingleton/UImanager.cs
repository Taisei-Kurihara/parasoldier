using Common;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class UImanager : Singleton_MonoBehaviourBase<UImanager>
{
    bool setUpCanvas = false;
    
    // Canvasを保持する変数
    private GameObject canvasObject;

    private void Awake()
    {
        AsyncInit().Forget();
    }

    async UniTask AsyncInit()
    {
        // Canvas_Blankプレハブをロードして生成
        var handle = Addressables.InstantiateAsync("Canvas_Blank");
        await handle.Task;
        canvasObject = handle.Result;
        
        // 生成したCanvasをこのオブジェクトの子として設定
        if (canvasObject != null)
        {
            canvasObject.transform.SetParent(transform);
        }
        
        setUpCanvas = true;
    }
    
    /// <summary>
    /// 外部からCanvas objectを親子付けできる関数
    /// SetUpCanvasがtrueになるまで待機してから実行
    /// </summary>
    /// <param name="childObject">親子付けするオブジェクト</param>
    public async UniTask AttachToCanvas(GameObject childObject)
    {
        // SetUpCanvasがtrueになるまで待機
        await UniTask.WaitUntil(() => setUpCanvas);
        
        if (canvasObject != null && childObject != null)
        {
            childObject.transform.SetParent(canvasObject.transform);
        }
    }
}
