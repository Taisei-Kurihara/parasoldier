using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Unity.VisualScripting.Antlr3.Runtime;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;
using System.Threading;

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
        nowPlayerCharacter = await LoadAndInstantiateAsync(PlayerCharacterName, new Vector3(-5, 1, 0), token);

        // HP UIの設定
        nowPlayerCharacter.GetComponent<CharacterResponseInput>().hp.Value = GameManager.Instance.HpManager.p1Hp.Value;
        nowPlayerCharacter.GetComponent<CharacterResponseInput>().hp.Subscribe(x => GameManager.Instance.HpManager.p1Hp.Value = x);

        GameSetUp().Forget();
    }



    async UniTask GameSetUp()
    {
        GameManager.Instance.HpManager.ResetHP(); // HPをリセット

        // プレイヤーキャラクターの位置とHPを設定
        nowPlayerCharacter.GetComponent<CharacterResponseInput>().hp.Value = GameManager.Instance.HpManager.p1Hp.Value;
        nowPlayerCharacter.transform.position = new Vector3(-5, 1, 0);

        await Enemy_StageSetUp();

        // 読み込みが完了したらシーンを更新
        SceneLoader loader = SceneLoader.Instance;
        loader.Loadended();

    }

    GameObject nowEnemy = null;
    GameObject nowStage = null;

    async UniTask Enemy_StageSetUp()
    {
        if (nowEnemy != null) { Destroy(nowEnemy); }
        if (nowStage != null) { Destroy(nowStage); }


        // 敵キャラクターの読み込み
        string enemyCharacterName = Datas.EnemyCharacterNames[fightNum].ToString();
        nowEnemy = await LoadAndInstantiateAsync(enemyCharacterName, new Vector3(5, 1, 0), token);

        // 反転させる
        Vector3 scale = nowEnemy.transform.localScale;
        scale.x = -1f;
        nowEnemy.transform.localScale = scale;

        // HP UIの設定
        nowEnemy.GetComponent<CharacterResponseInput>().hp.Value = GameManager.Instance.HpManager.p2Hp.Value;
        nowEnemy.GetComponent<CharacterResponseInput>().hp.Subscribe(x => GameManager.Instance.HpManager.p2Hp.Value = x);



        // ステージの読み込み
        string StageName = Datas.StageName.ToString();

        // Identityステージの場合、敵のIdentityStageを取得
        if (StageName == SelectStage.Identity.ToString()) 
        {
            StageName = nowEnemy.GetComponent<EnemyAI>().IdentityStage.ToString();
            await UniTask.WaitUntil(() => StageName != SelectStage.Identity.ToString(), cancellationToken: token);
        }

        nowStage = await LoadAndInstantiateAsync(StageName,Vector3.down, token);

        fightNum++;
    }


    public async UniTask<GameObject> LoadAndInstantiateAsync(string address,Vector3 InsPos, CancellationToken cancellationToken)
    {
        // アセットロード
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);
        await handle.ToUniTask(cancellationToken: cancellationToken);  // キャンセル対応

        // キャンセルされた場合はここで例外が投げられる
        if (cancellationToken.IsCancellationRequested)
        {
            Addressables.Release(handle); // メモリリーク防止のため必ず Release
            return null;
        }

        GameObject prefab = handle.Result;
        GameObject instance = Instantiate(prefab, InsPos,Quaternion.identity);
        // 注意: インスタンス自体の管理は呼び出し元で行うべき
        Addressables.Release(handle); // Prefab の参照だけなので解放してもOK
        return instance;
    }
}
