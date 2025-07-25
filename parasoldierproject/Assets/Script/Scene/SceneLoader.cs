using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public enum SceneName
{
    Title,
    Select,
    Game,
    Result
}

public enum LoadSceneName
{
    LongLoadScene,
    ShortLoadScene,
    Load
}

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader instance { get; private set; }

    //private static SceneLoader instance;

    public static SceneLoader Instance
    {
        get
        {
            if (instance == null)
            {
                // 既存のインスタンスを探す
                instance = FindObjectOfType<SceneLoader>();

                // なければ新規生成
                if (instance == null)
                {
                    GameObject obj = new GameObject("SceneLoader");
                    instance = obj.AddComponent<SceneLoader>();
                }
            }
            return instance;
        }
    }

    string nowLoadingSceneName = string.Empty;


    void Awake()
    {
        // シングルトン化
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); // シーンをまたいでも保持
        nowLoadingSceneName = SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// 指定したシーン名で非同期にシーンを読み込む
    /// </summary>
    public void LoadNextScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName,LoadSceneName.Load.ToString()));
    }

    /// <summary>
    /// 現在のシーンをリロード
    /// </summary>
    public void ReloadScene()
    {
        StartCoroutine(LoadSceneAsync(SceneManager.GetActiveScene().name, LoadSceneName.Load.ToString()));
    }


    
    private float percent = 1;

    public float Percent { get { return percent; } }

    private BitArray loadFlags = new BitArray(4,true);

    private void LoadFlagsReset() { loadFlags = new BitArray(4, false); }
    public void LoadUIAllSet(){ loadFlags[0] = true; }
    public void Loadended() { loadFlags[2] = true; }
    public void LoadAllClear() { loadFlags[3] = true; }

    public bool CheckLoadended { get { return loadFlags[2]; } }
    public bool CheckLoadAllClear { get { return loadFlags[3]; } }
    // [0] = ロードUI準備完了
    // [1] = 現在のシーン削除済み
    // [2] = 次シーン読み込み完了（が、まだ時間停止中）
    // [3] = 全て完了して時間再開

    IEnumerator LoadSceneAsync(string nextSceneName, string loadSceneName)
    {

        // 変数初期化
        percent = 0;
        LoadFlagsReset();

        // 1. ロードUI表示
        yield return SceneManager.LoadSceneAsync(loadSceneName, LoadSceneMode.Additive);


        // 2. ロードUI準備完了を待つ
        yield return new WaitUntil(() => loadFlags[0]);

        // 3. 現在のシーン削除
        yield return SceneManager.UnloadSceneAsync(nowLoadingSceneName);
        nowLoadingSceneName = nextSceneName; // 次のシーン名を更新
        loadFlags.Set(1, true);

        // 4. 次のシーン読み込み、時間停止
        var nextOp = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
        nextOp.allowSceneActivation = false;

        while (percent < 1){ percent = Mathf.Clamp01(nextOp.progress / 0.9f); }
        nextOp.allowSceneActivation = true;
        while (!nextOp.isDone) yield return null;

        CreativeDestructionManager manager = CreativeDestructionManager.Instance;
        manager.WhatToDoNow(nextSceneName);
        //loadFlags.Set(2, true);

        // 5. ロードUIの終了通知を待つ
        while (!loadFlags.Get(3)) yield return null;
        SceneManager.UnloadSceneAsync(loadSceneName);
    }

    public void ShowOnlyLoadUI()
    {
        StartCoroutine(ShowLoadUIOnlyRoutine());
    }

    private IEnumerator ShowLoadUIOnlyRoutine()
    {
        yield return null;
        LoadFlagsReset();
        // ロードUIだけ出す
        yield return SceneManager.LoadSceneAsync(LoadSceneName.Load.ToString(), LoadSceneMode.Additive);
        percent = 1;
        loadFlags[1] = true;

        while (!loadFlags.Get(3)) yield return null;
        SceneManager.UnloadSceneAsync(LoadSceneName.Load.ToString());
    }

}