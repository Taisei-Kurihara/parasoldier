using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Unity.VisualScripting.Antlr3.Runtime;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using Cysharp.Threading.Tasks.Triggers;
using System.Linq;
using System.Collections.Generic;

public class GameSetUp_FlowManager : MonoBehaviour
{
    #region singleton
    public static GameSetUp_FlowManager instance { get; private set; }

    public static GameSetUp_FlowManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameSetUp_FlowManager>();

                if (instance == null)
                {
                    GameObject obj = new GameObject("CharacterAndStageGenerator");
                    instance = obj.AddComponent<GameSetUp_FlowManager>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {;

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    #endregion

    int fightNum = 0;

    CreativeCharacterAndStageDatas Datas;

    public void FightSetUp(CreativeCharacterAndStageDatas Datas)
    {
        if(Datas == null) { Debug.LogError("da null"); }
        this.Datas = Datas;

        fightNum = 0;

        Init().Forget();
    }

    CancellationToken token;

    GameObject nowPlayerCharacter = null;

    // シーン読み込み時の一度だけ行う処理
    async UniTask Init()
    {
        token = this.destroyCancellationToken;

        // プレイヤーキャラクターの読み込み
        string PlayerCharacterName = Datas.PlayerCharacterName.ToString();
        nowPlayerCharacter = await AddreLoadAndInstantiateAsync(PlayerCharacterName, new Vector3(-5, 1, 0), token);
        
        var playerStatus = nowPlayerCharacter.GetComponent<CharacterStatus>();
        SyncCharacterStatus(playerStatus, PType.P1);

        GameSetUp().Forget();
    }


    public void retry()
    {
        // ロード画面のみを再度出す
        SceneLoader.Instance.ShowOnlyLoadUI();
        GameSetUp().Forget();
    }

    async UniTask GameSetUp()
    {

        await Enemy_StageSetUp().AttachExternalCancellation(token);

        GameManager.Instance.HpManager.ResetHP();

        var playerMove = nowPlayerCharacter.GetComponent<CharacterMove>();
        var enemyMove = nowEnemy.GetComponent<CharacterMove>();
        playerMove.DisableInput(); // ← 戦闘前に移動・入力禁止
        enemyMove.DisableInput(); // ← 戦闘前に移動・入力禁止

        nowPlayerCharacter.GetComponent<CharacterStatus>().hp.Value = GameManager.Instance.HpManager.p1Hp.Value;
        nowPlayerCharacter.GetComponent<CharacterStatus>().gage.Value = GameManager.Instance.HpManager.p1Gage.Value;
        nowPlayerCharacter.transform.position = new Vector3(-5, 1, 0);

        // ReadyFight演出
        await ShowReadyFight().AttachExternalCancellation(token);

        playerMove.UnlockInput();
        enemyMove.UnlockInput();

        SceneLoader.Instance.Loadended(); // ロードUI解除
    }

    private void Update()
    {
        if (nowPlayerCharacter != null)
        {
            string debugInfo = "";

            // 位置
            debugInfo += $"p_pos: {nowPlayerCharacter.transform.position}\n";

            // Scale
            debugInfo += $"scale: {nowPlayerCharacter.transform.lossyScale}\n";

            // アクティブ状態
            debugInfo += $"active: {nowPlayerCharacter.activeInHierarchy}\n";

            // MeshRendererまたはSkinnedMeshRendererを取得
            Renderer renderer = nowPlayerCharacter.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Material mat = renderer.sharedMaterial;
                if (mat != null)
                {
                    debugInfo += $"material: {mat.name}\n";
                    debugInfo += $"shader: {mat.shader?.name ?? "null"}\n";
                    debugInfo += $"albedoTex: {(mat.HasProperty("_MainTex") ? mat.mainTexture?.name ?? "null" : "no _MainTex")}\n";
                    debugInfo += $"renderQueue: {mat.renderQueue}\n";
                }
                else
                {
                    debugInfo += "material: null\n";
                }
            }
            else
            {
                debugInfo += "renderer: null\n";
            }

            // 表示
            texttest.Instance.text = debugInfo;
        }
    }

    GameObject readyFightInstance = null;

    async UniTask ShowReadyFight()
    {
        if (readyFightInstance == null)
        {
            var handle = await AddreLoadAndInstantiateAsync("ReadyFight", Vector3.zero, token);
            readyFightInstance = handle;

            readyFightInstance.transform.parent = CreativeDestructionManager.Instance.MainCanvasData.transform;
            readyFightInstance.transform.localPosition = Vector3.zero; // Canvasの中心に配置
        }
        else
        {
            readyFightInstance.SetActive(true); // 既に存在する場合は表示
        }

        SceneLoader.Instance.Loadended(); // ロードUI解除

        await UniTask.Delay(1000); // 表示演出用の待機など

        // ロード解除後2秒後に入力解放
        await UniTask.Delay(2000);

        readyFightInstance.SetActive(false); // ReadyFight画像を非表示にする
    }
    public bool IsLastEnemy => (fightNum >= Datas.EnemyCharacterNames.Count());

    GameObject nowEnemy = null;
    GameObject nowStage = null;

    async UniTask Enemy_StageSetUp()
    {
        if (nowEnemy != null) { Destroy(nowEnemy); }
        if (nowStage != null) { Destroy(nowStage); }


        // 敵キャラクターの読み込み
        string enemyCharacterName = Datas.EnemyCharacterNames[fightNum].ToString();
        nowEnemy = await AddreLoadAndInstantiateAsync(enemyCharacterName, new Vector3(5, 1, 0), token);

        // 反転させる
        Vector3 scale = nowEnemy.transform.localScale;
        scale.x = -1f;
        nowEnemy.transform.localScale = scale;

        var enemyAIStatus = nowEnemy.GetComponent<EnemyAIBase>();
        enemyAIStatus.SetAiLevel(enemyAILv.CombatLv1);
        enemyAIStatus.Lv();

        var enemyStatus = nowEnemy.GetComponent<CharacterStatus>();
        SyncCharacterStatus(enemyStatus, PType.P2);

        // ステージの読み込み
        string StageName = Datas.StageName.ToString();

        // Identityステージの場合、敵のIdentityStageを取得
        if (StageName == SelectStage.Identity.ToString()) 
        {
            StageName = nowEnemy.GetComponent<EnemyAIBase>().IdentityStage.ToString();
            await UniTask.WaitUntil(() => StageName != SelectStage.Identity.ToString(), cancellationToken: token);
        }

        nowStage = await AddreLoadAndInstantiateAsync(StageName,Vector3.down, token);

        fightNum++;
    }



    private void SyncCharacterStatus(CharacterStatus status, PType type)
    {
        var hpManager = GameManager.Instance.HpManager;
        var gameManager = GameManager.Instance;

        status.OwnerType = type;

        if (type == PType.P1)
        {
            status.hp.Value = hpManager.p1Hp.Value;
            status.hp.Subscribe(x => hpManager.p1Hp.Value = x).AddTo(status);

            status.gage.Value = hpManager.p1Gage.Value;
            status.gage.Subscribe(x => hpManager.p1Gage.Value = x).AddTo(status);

            // ← 状態監視をGameManagerに接続
            status.currentState.Subscribe(x => gameManager.p1State.Value = x).AddTo(status);
            GameManager.Instance.PlayerTransform = status.transform;
        }
        else if (type == PType.P2)
        {
            status.hp.Value = hpManager.p2Hp.Value;
            status.hp.Subscribe(x => hpManager.p2Hp.Value = x).AddTo(status);

            status.gage.Value = hpManager.p2Gage.Value;
            status.gage.Subscribe(x => hpManager.p2Gage.Value = x).AddTo(status);

            // ← 状態監視をGameManagerに接続
            status.currentState.Subscribe(x => gameManager.p2State.Value = x).AddTo(status);
            GameManager.Instance.EnemyTransform = status.transform;
        }
    }

    public async UniTask<List<GameObject>> AddreLoadAndInstantiateMultipleAsync(string address, int count,Vector3 basePosition,Vector3 offset,Transform parent = null,CancellationToken cancellationToken = default)
    {
        List<GameObject> instances = new List<GameObject>();

        GameObject prefab = await AddreLoadAsync(address, cancellationToken).AttachExternalCancellation(cancellationToken);
        if (prefab == null) return instances;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = basePosition + offset * i;
            GameObject instance = Object.Instantiate(prefab, pos, Quaternion.identity, parent);
            instances.Add(instance);
        }

        return instances;
    }

    public async UniTask<GameObject> AddreLoadAndInstantiateAsync(string address, Vector3 insPos, CancellationToken cancellationToken = default)
    {
        GameObject instance = await AddreLoadAndInstantiateAsync(address, insPos, (Transform)null, cancellationToken).AttachExternalCancellation(cancellationToken);
        return instance;
    }


    public async UniTask<GameObject> AddreLoadAndInstantiateAsync(string address, Vector3 insPos, Transform parent, CancellationToken cancellationToken = default)
    {
        GameObject prefab = await AddreLoadAsync(address, cancellationToken).AttachExternalCancellation(cancellationToken);

        if (prefab == null) return null;

        // 親子付けして生成
        GameObject instance = Object.Instantiate(prefab, insPos, Quaternion.identity, parent);
        return instance;
    }

    public async UniTask<GameObject> AddreLoadAsync(string address, CancellationToken cancellationToken = default)
    {
        // アセットロード
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
        await UniTask.WaitUntil(() => handle.Result !=null ,PlayerLoopTiming.EarlyUpdate,cancellationToken);

        //// キャンセルされた場合はここで例外が投げられる
        //if (cancellationToken.IsCancellationRequested)
        //{
        //    Addressables.Release(handle); // メモリリーク防止のため必ず Release
        //    return null;
        //}

        GameObject instance = handle.Result;

        //Addressables.Release(handle); // Prefab の参照だけなので解放してもOK
        return instance;
    }

}
