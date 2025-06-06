using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System.Collections;
using static UnityEngine.Rendering.DebugUI.Table;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Windows;
using Unity.VisualScripting;

public enum ImplementedPlayerCharacter
{
    PlayerTestCharacter = 0,
}

public class CreativeDestructionManager : MonoBehaviour
{
    #region singleton
    public static CreativeDestructionManager instance { get; private set; }

    public static CreativeDestructionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CreativeDestructionManager>();

                if (instance == null)
                {
                    GameObject obj = new GameObject("CreativeDestructionManager");
                    instance = obj.AddComponent<CreativeDestructionManager>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        DebugSceneCheck();
#endif
    }
    #endregion

    #region
    private void DebugSceneCheck()
    {
        SceneName sceneName;
        if (SceneManager.GetActiveScene().name == "Title")
        {
            sceneName = SceneName.Title;
        }
        else if (SceneManager.GetActiveScene().name == "Select")
        {
            sceneName = SceneName.Select;
        }
        else if (SceneManager.GetActiveScene().name == "Game")
        {
            sceneName = SceneName.Game;
        }
        else
        {
            sceneName = SceneName.Result;
        }
        WhatToDoNow(sceneName.ToString());
    }

    #endregion

    #region

    /// <summary> 次のscene読み込み時 特定の処理を行ってからloadを開けるようにする /// </summary>
    /// <param name="sceneName"></param>
    public void WhatToDoNow(string sceneName)
    {

        SceneLoader loader = SceneLoader.Instance;

        mainCanvas = null;

        if (sceneName == SceneName.Title.ToString())
        {
            loader.Loadended();
        }
        else if (sceneName == SceneName.Select.ToString())
        {
            StartPlayerCharacterMenu().Forget();
        }
        else if (sceneName == SceneName.Game.ToString())
        {
        }
        else if(sceneName == SceneName.Result.ToString())
        {
        }
    }


    #endregion

    CreativeCharacterAndStageDatas Datas;
    int playerCharacterIndex = 0; // プレイヤーキャラクターのインデックス


    MainCanvas mainCanvas = null;


    public MainCanvas MainCanvasData { get { return mainCanvas; } set { mainCanvas = (mainCanvas = null) ? null : mainCanvas; } }

    #region

    /// <summary> selectボタンの生成配置 /// </summary>
    async UniTask StartPlayerCharacterMenu()
    {
        
        int i = 0;

        await UniTask.WaitUntil(() => mainCanvas != null, cancellationToken: this.destroyCancellationToken);
        
        // selectボタンを生成する
        string addressKey = "key";

        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(addressKey);
        await handle;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"Successfully loaded: {addressKey}");
            while (Enum.IsDefined(typeof(ImplementedPlayerCharacter), i))
            {
                Debug.Log($"Loading character {i} from address: {addressKey}");
                GameObject prefab = handle.Result;
                GameObject instance = Instantiate(prefab, mainCanvas.transform);
                SelectButton selectButton = instance.GetComponent<SelectButton>();

                selectButton.Init(i);
                i++;
            }
        }
        else
        {
            Debug.LogWarning($"Failed to load: {addressKey}");
        }

        SetCheck().Forget();
    }


    BitArray StartOrBackCheck = new BitArray(1, true);

    public bool StartButtonInputCheck => StartOrBackCheck[0];

    public void StartButtonInput() { StartAsync().Forget(); }

    public void BackButtonInput() { Back(); }

    async UniTask SetCheck()
    {
        var token = this.destroyCancellationToken;

        SceneLoader loader = SceneLoader.Instance;
        loader.Loadended();

        StartOrBackCheck[0] = false;
    }

    async UniTask StartAsync()
    {
        var token = this.destroyCancellationToken;

        StartOrBackCheck[0] = true;
        Datas = new CreativeCharacterAndStageDatas(new string[0], new string[0], Enum.ToObject(typeof(ImplementedPlayerCharacter), playerCharacterIndex).ToString());

        SceneLoader.Instance.LoadNextScene(SceneName.Game.ToString());
    }

    void Back()
    {
        StartOrBackCheck[0] = true;
        SceneLoader.Instance.LoadNextScene(SceneName.Title.ToString());
    }
    #endregion


}

public class CreativeCharacterAndStageDatas
{
    string[] stageName;
    string playerCharacterName;
    string[] enemyCharacterNames;

    public string[] StageName => stageName;
    public string PlayerCharacterName => playerCharacterName;
    public string[] EnemyCharacterNames => enemyCharacterNames;


    public CreativeCharacterAndStageDatas(string[] stageName,string[] EnemyCharacterNames, string playerCharacterNames)
    {
        this.stageName = stageName;
        this.playerCharacterName = playerCharacterNames;
        this.enemyCharacterNames = EnemyCharacterNames;
    }
}

