using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Unity.VisualScripting.Antlr3.Runtime;

public class CharacterAndStageGenerator : MonoBehaviour
{
    #region singleton
    public static CharacterAndStageGenerator instance { get; private set; }

    public static CharacterAndStageGenerator Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CharacterAndStageGenerator>();

                if (instance == null)
                {
                    GameObject obj = new GameObject("CharacterAndStageGenerator");
                    instance = obj.AddComponent<CharacterAndStageGenerator>();
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
        
    }
    #endregion

    int fightNum = 0;

    CreativeCharacterAndStageDatas Datas;
    public CreativeCharacterAndStageDatas SetfightDatas
    {
        set {
            if (value == null)
            {
                Debug.LogError("value null");
            }
            Datas = value; 
            fightNum = 0;
            Init().Forget();
        } 
    }

    GameObject nowPlayerCharacter = null;

    // シーン読み込み時の一度だけ行う処理
    async UniTask Init()
    {

        string PlayerName = Datas.PlayerCharacterName.ToString();

        AsyncOperationHandle<GameObject> Playerhandle = Addressables.LoadAssetAsync<GameObject>(PlayerName);
        await Playerhandle;

        nowPlayerCharacter = Instantiate(Playerhandle.Result, new Vector3(-5,1,0), Quaternion.identity);

        nowPlayerCharacter.GetComponent<CharacterResponseInput>().hp.Value = GameManager.Instance.HpManager.p1Hp.Value;

        nowPlayerCharacter.GetComponent<CharacterResponseInput>().hp.Subscribe(x => GameManager.Instance.HpManager.p1Hp.Value = x);

        await EnemyAndStageReplace();
    }

    GameObject nowEnemy = null;
    GameObject nowStage = null;

    private async UniTask EnemyAndStageReplace()
    {
        var token = this.destroyCancellationToken;

        if (nowEnemy != null) { Destroy(nowEnemy); }
        if(nowStage != null) { Destroy(nowStage); }


        // 敵キャラクターの読み込み
        string EnemyName = Datas.EnemyCharacterNames[fightNum].ToString();

        AsyncOperationHandle<GameObject> Enemyhandle = Addressables.LoadAssetAsync<GameObject>(EnemyName);
        await Enemyhandle;

        // 敵キャラクターのインスタンス化
        nowEnemy = Instantiate(Enemyhandle.Result, new Vector3(5, 1, 0), Quaternion.identity);
        
        await UniTask.WaitUntil(() => nowEnemy != null, cancellationToken: token);

        // 反転させる
        Vector3 scale = nowEnemy.transform.localScale;
        scale.x = -1f;
        nowEnemy.transform.localScale = scale;


        // 体力の同期
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

        AsyncOperationHandle<GameObject> Stagehandle = Addressables.LoadAssetAsync<GameObject>(StageName);
        await Stagehandle;

        // ステージのインスタンス化
        nowStage = Instantiate(Stagehandle.Result, new Vector3(0, -1, 0), Quaternion.identity);

        await UniTask.WaitUntil(() => nowStage != null, cancellationToken: token);

        // ステージの読み込みが完了したらシーンを更新
        SceneLoader loader = SceneLoader.Instance;
        loader.Loadended();


        fightNum++;
    }
}
