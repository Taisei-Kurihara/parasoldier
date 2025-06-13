using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Unity.VisualScripting.Antlr3.Runtime;

public class CharacterAndStageGenerator : MonoBehaviour
{ 

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

        await EnemyAndStageReplace();
    }

    GameObject nowEnemy = null;
    GameObject nowStage = null;

    private async UniTask EnemyAndStageReplace()
    {
        var token = this.destroyCancellationToken;

        if (nowEnemy != null) { Destroy(nowEnemy); }
        if(nowStage != null) { Destroy(nowStage); }

        string EnemyName = Datas.EnemyCharacterNames[fightNum].ToString();

        AsyncOperationHandle<GameObject> Enemyhandle = Addressables.LoadAssetAsync<GameObject>(EnemyName);
        await Enemyhandle;

        nowEnemy = Instantiate(Enemyhandle.Result, new Vector3(5, 1, 0), Quaternion.identity);




        string StageName = Datas.StageName.ToString();

        if (StageName == SelectStage.Identity.ToString()) 
        {
            StageName = nowEnemy.GetComponent<EnemyAI>().IdentityStage.ToString();
        }

        Debug.Log("stage name await");

        await UniTask.WaitUntil(() => StageName != SelectStage.Identity.ToString(), cancellationToken: token);

        Debug.Log("stage name emd await");

        AsyncOperationHandle<GameObject> Stagehandle = Addressables.LoadAssetAsync<GameObject>(StageName);
        await Stagehandle;

        nowStage = Instantiate(Stagehandle.Result, new Vector3(0, -1, 0), Quaternion.identity);

        SceneLoader loader = SceneLoader.Instance;
        loader.Loadended();


        fightNum++;
    }
}
