using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System.Collections;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Windows;
using Unity.VisualScripting;

public enum ImplementedPlayerCharacter
{
    PlayerTestCharacter = 0,
}

public enum ImplementedEnemyCharacter
{
    EnemyTestCharacter = 0,
}

public enum SelectStage
{
    Identity,
    TestStage,
    RojouStage
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
        //DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        //DebugSceneCheck();
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
    public void WhatToDoNow(string sceneName)
    {

        SceneLoader loader = SceneLoader.Instance;

        mainCanvas = null;

        Debug.Log($"WhatToDoNow called with sceneName: {sceneName}");

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
            StartGameObjectSet().Forget();
        }
        else if(sceneName == SceneName.Result.ToString())
        {
        }
    }


    #endregion

    CreativeCharacterAndStageDatas Datas;
    int playerCharacterIndex = 0; // プレイヤーキャラクターのインデックス

    SelectButton nowSelectButton = null; // 現在選択されているボタン
    public SelectButton NowSelectButton { get { return nowSelectButton; } set { nowSelectButton.AwaitButton(); nowSelectButton = value; playerCharacterIndex = value.GetNum; } }


    MainCanvas mainCanvas = null;

    public MainCanvas MainCanvasData { get { return mainCanvas; } set { mainCanvas = value; } }

    #region

    /// <summary> selectボタンの生成配置 /// </summary>
    async UniTask StartPlayerCharacterMenu()
    {

        int i = 0;

        Debug.Log("Starting player character menu setup...");

        await UniTask.WaitUntil(() => mainCanvas != null, cancellationToken: this.destroyCancellationToken);

        Debug.Log("MainCanvas is ready, proceeding to load character buttons...");

        // selectボタンを生成する
        string addressKey = "selectButton";

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
                if (i == 0)
                {
                    nowSelectButton = selectButton;
                }
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
        StartOrBackCheck[0] = true;
   
        SceneLoader.Instance.LoadNextScene(SceneName.Game.ToString());
    }

    void Back()
    {
        StartOrBackCheck[0] = true;
        SceneLoader.Instance.LoadNextScene(SceneName.Title.ToString());
    }
    #endregion


    /// <summary> ゲーム開始時生成処理 </summary>
    async UniTask StartGameObjectSet()
    {
        // Gameシーンを取得してアクティブにする（すでにロード済みであることが前提）
        Scene gameScene = SceneManager.GetSceneByName("Game");
        if (gameScene.IsValid() && gameScene.isLoaded)
        {
            SceneManager.SetActiveScene(gameScene);
        }
        else
        {
            Debug.LogError("Gameシーンがロードされていません。");
            return;
        }

        SceneLoader loader = SceneLoader.Instance;

        int count = Enum.GetValues(typeof(ImplementedEnemyCharacter)).Length;

        CharacterAndStageGenerator.Instance.SetfightDatas = new CreativeCharacterAndStageDatas(
            SelectStage.Identity,
            new[] { ImplementedEnemyCharacter.EnemyTestCharacter },
            (ImplementedPlayerCharacter)Enum.ToObject(typeof(ImplementedPlayerCharacter), playerCharacterIndex)
        ); ;

        loader.Loadended();
    }
}

public class CreativeCharacterAndStageDatas
{
    SelectStage stageName;
    ImplementedPlayerCharacter playerCharacterName;
    ImplementedEnemyCharacter[] enemyCharacterNames;

    public SelectStage StageName { get { return stageName; } }
    public ImplementedPlayerCharacter PlayerCharacterName { get { return playerCharacterName; } }
    public ImplementedEnemyCharacter[] EnemyCharacterNames { get { return enemyCharacterNames; } }


    public CreativeCharacterAndStageDatas(SelectStage stageName, ImplementedEnemyCharacter[] EnemyCharacterNames, ImplementedPlayerCharacter playerCharacterNames)
    {
        this.stageName = stageName;
        this.playerCharacterName = playerCharacterNames;
        this.enemyCharacterNames = EnemyCharacterNames;
    }
}

