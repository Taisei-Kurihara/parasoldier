using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections;
using static UnityEngine.Rendering.DebugUI.Table;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Windows;

public enum ImplementedPlayerCharacter
{
    
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
                // 既存のインスタンスを探す
                instance = FindObjectOfType<CreativeDestructionManager>();

                // なければ新規生成
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
        // シングルトン化
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); // シーンをまたいでも保持

        // エディタ上でのデバッグ用
#if UNITY_EDITOR
        DebugSceneCheck();
#endif
    }
    #endregion

    #region エディタ上でのデバッグ用
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

    #region scene事の処理を分岐 / 開始命令処理

    // scene事の処理を分岐メソッド
    public void WhatToDoNow(string sceneName)
    {
        if (sceneName == SceneName.Title.ToString())
        {
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

    /// <summary> キャラクターとステージのデータを保持するクラス /// </summary>
    CreativeCharacterAndStageDatas Datas;

    #region メニュー画面の処理

    async UniTask StartPlayerCharacterMenu()
    {
        SetCheck().Forget(); // スタートボタンとバックボタンの入力チェックを開始
    }

    BitArray StartOrBackCheck = new BitArray(2,false);

    public void StartButtonInput() { StartOrBackCheck[0] = true; }

    public void BackButtonInput() { StartOrBackCheck[1] = true; }

    async UniTask SetCheck()
    {
        var token = this.destroyCancellationToken;

        await UniTask.WaitUntil(() => StartOrBackCheck != new BitArray(2, false), cancellationToken: token);

        if (StartOrBackCheck[0])
        {
            StartAsync().Forget();
        }
        else
        {
            Back();
        }
    }

    async UniTask StartAsync()
    {
        var token = this.destroyCancellationToken;
        Debug.Log("ゲーム開始");
    }

    void Back()
    {
        var token = this.destroyCancellationToken;
        Debug.Log("タイトルに戻る");
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

