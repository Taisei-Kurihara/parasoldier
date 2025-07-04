using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

/// <summary> ゲームの進行 </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 既存のインスタンスを探す
                instance = FindObjectOfType<GameManager>();

                // なければ新規生成
                if (instance == null)
                {
                    GameObject obj = new GameObject("GameManager");
                    instance = obj.AddComponent<GameManager>();
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
    }

    HPManager hpManager;
    public HPManager HpManager { get { return hpManager; } set { hpManager = value; } }
    public int orHPzero => hpManager.orHPzero.Value;

    public ReactiveProperty<CharacterState> p1State = new(CharacterState.Idle);
    public ReactiveProperty<CharacterState> p2State = new(CharacterState.Idle);

    public Transform PlayerTransform { get; set; }
    public Transform EnemyTransform { get; set; }

    public void HpZeroCheck()
    {
        hpManager.orHPzero
            .Where(isZero => isZero == 1 || isZero == 2) // ← 条件を満たすものだけ通す
            .Take(1) // ← 最初の1回で購読解除
            .Subscribe(isZero =>
            {
                // 両キャラの入力を停止
                PlayerTransform.GetComponent<CharacterMove>()?.DisableInput();
                EnemyTransform.GetComponent<CharacterMove>()?.DisableInput();

                // リザルト演出開始
                ShowResultFlow("ResultImage").Forget();
            })
            .AddTo(this);

    }

    GameObject nowresul { get { return nowresul; } set { Destroy(nowresul); nowresul = value; } }

    private async UniTask ShowResultFlow(string result)
    {
        var token = this.GetCancellationTokenOnDestroy();
        
        nowresul = await GameSetUp_FlowManager.Instance.AddreLoadAndInstantiateAsync(result, Vector3.zero ,token);

        nowresul.SetActive(false); // 生成したオブジェクトを有効化

        nowresul.transform.parent = CreativeDestructionManager.Instance.MainCanvasData.transform;
        nowresul.transform.localPosition = Vector3.zero; // Canvasの中心に配置

        nowresul.SetActive(true); // リザルト画面を表示
    }

    public void OnRetryButton()
    {
        // 再戦演出後にセットアップ
        RetrySetUp().Forget();
    }

    private async UniTask RetrySetUp()
    {
        var token = this.GetCancellationTokenOnDestroy();

        // 戦闘継続できるかチェック
        if (GameSetUp_FlowManager.Instance.IsLastEnemy)
        {
            // 最終結果画面表示
            var finalResult = await GameSetUp_FlowManager.Instance.AddreLoadAndInstantiateAsync("FinalResult", Vector3.zero, this.GetCancellationTokenOnDestroy());
            finalResult.transform.SetParent(CreativeDestructionManager.Instance.MainCanvasData.transform);
            finalResult.transform.localPosition = Vector3.zero;
        }
        else
        {

            GameSetUp_FlowManager.Instance.retry();
        }
        // SetUp呼び出し
    }





}
