using R3;
using UnityEngine;

public class UnityFrameProvider : FrameProvider
{
    public override void Register(IFrameRunnerWorkItem callback)
    {
        // Unity の Update フレームに同期して実行
        UnityFrameProviderBootstrap.Instance.RegisterCallback(callback);
    }

    public override long GetFrameCount()
    {
        // Unity の Time.frameCount を返す
        return Time.frameCount;
    }
}

public class UnityFrameProviderBootstrap : MonoBehaviour
{
    public static UnityFrameProviderBootstrap Instance { get; private set; }
    
    void Awake()
    {
        Instance = this;
        // このクラスを FrameProvider として登録
        ObservableSystem.DefaultFrameProvider = new UnityFrameProvider();
    }

    // R3 がフレームごとに呼び出す処理をここで実行する
    public void RegisterCallback(IFrameRunnerWorkItem callback)
    {
        // MonoBehaviour の Update にフックする
        StartCoroutine(Run(callback));
    }

    private System.Collections.IEnumerator Run(IFrameRunnerWorkItem callback)
    {
        while (true)
        {
            yield return null; // 1フレーム待つ
            callback.MoveNext(Time.frameCount);
        }
    }
}