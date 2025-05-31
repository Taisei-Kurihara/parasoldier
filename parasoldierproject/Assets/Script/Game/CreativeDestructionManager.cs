using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
                // �����̃C���X�^���X��T��
                instance = FindObjectOfType<CreativeDestructionManager>();

                // �Ȃ���ΐV�K����
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
        // �V���O���g����
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); // �V�[�����܂����ł��ێ�

        // �G�f�B�^��ł̃f�o�b�O�p
#if UNITY_EDITOR
        DebugSceneCheck();
#endif
    }
    #endregion

    #region �G�f�B�^��ł̃f�o�b�O�p
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

    #region scene���̏����𕪊� / �J�n���ߏ���

    // scene���̏����𕪊򃁃\�b�h
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

    /// <summary> �L�����N�^�[�ƃX�e�[�W�̃f�[�^��ێ�����N���X /// </summary>
    CreativeCharacterAndStageDatas Datas;

    MainCanvas mainCanvas = null;

    /// <summary> [0 = �{�^����������Ă��Ȃ���] [1 = start] [2 = �߂�] /// </summary>
    BitArray StartOrBackCheck = new BitArray(3, true);

    public MainCanvas MainCanvas { get { return mainCanvas; } set { mainCanvas = (mainCanvas = null) ? null : mainCanvas; } }

    #region ���j���[��ʂ̏���

    async UniTask StartPlayerCharacterMenu()
    {
        
        int i = 0;

        while (Enum.IsDefined(typeof(ImplementedPlayerCharacter), i))
        {
            string addressKey = "key";

            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(addressKey);
            await handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject prefab = handle.Result;
                GameObject instance = Instantiate(prefab, mainCanvas.transform);
            }
            else
            {
                Debug.LogWarning($"Failed to load: {addressKey}");
            }

            i++;
        }

        SetCheck().Forget(); // �X�^�[�g�{�^���ƃo�b�N�{�^���̓��̓`�F�b�N���J�n
    }


    public void StartButtonInput() { StartOrBackCheck[1] = true; }

    public void BackButtonInput() { StartOrBackCheck[2] = true; }

    async UniTask SetCheck()
    {
        var token = this.destroyCancellationToken;

        SceneLoader loader = SceneLoader.Instance;
        loader.Loadended();

        // ���j���[��ʂ̏���������
        if (StartOrBackCheck[0]) { StartOrBackCheck = new BitArray(3, false); }

        await UniTask.WaitUntil(() => StartOrBackCheck == new BitArray(3, false), cancellationToken: token);

        StartOrBackCheck[0] = true;

        if (StartOrBackCheck[1])
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
        Debug.Log("�Q�[���J�n");
    }

    void Back()
    {
        var token = this.destroyCancellationToken;
        Debug.Log("�^�C�g���ɖ߂�");
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

