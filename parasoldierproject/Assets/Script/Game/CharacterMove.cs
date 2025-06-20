using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using Unity.VisualScripting;
using UnityEngine;

public enum CharacterState
{
    Idle,
    Move,
    Attack,
    WaterShot,
    Charge,
    Assault,
    Guard,
    DamageReaction
}

/// <summary> キャラクタの動きだけを管理する </summary>
public class CharacterMove : MonoBehaviour
{
    [SerializeField]
    Animator animator;

    [SerializeField]
    Collider attackColl;

    CancellationToken token;
    Rigidbody rigidbody;


    #region 入力管理

    #region 全体
    CharacterState NowState = CharacterState.Idle;

    BitArray generalOrder = new BitArray(2, true);

    /// <summary> 戦闘前or決着後に動けなくする用 </summary>
    public bool inputFailure { get { return generalOrder[0]; } }
    private void UnlockInput() { generalOrder[0] = false; }
    private void DisableInput() { generalOrder[0] = true; }

    // / <summary> 入力割り込みできるか </summary>
    public bool Interrupt { get { return (inputFailure) ? inputFailure:generalOrder[1]; } set { generalOrder[1] = value; } }

    #endregion

    #region 移動入力管理

    BitArray moveInput = new BitArray(2, false);

    /// <summary> 現在移動入力中かどうか </summary>
    public bool NowMoveInput { get { return moveInput[0]; } }
    private bool SetNowMoveInput { set { moveInput[0] = value; } }


    /// <summary> 移動可能な状態か確認する </summary>
    public bool CanItBeMoved { get { return moveInput[1]; } }
    private void UnMove() { moveInput[1] = false; }
    private void OnMove() { moveInput[1] = true; }
    #endregion

    #region 攻撃入力管理

    const int maxCombo = 3;

    int nowComboIndex = 0; // 現在のコンボインデックス

    void AttackComboReset() { nowComboIndex = 0; }

    private int NowCombo { get { return (nowComboIndex + 1); } }

    void NextCombo()
    {
        nowComboIndex = (nowComboIndex + 1) % maxCombo;
    }

    #endregion

    #endregion


    private void Awake()
    {
        token = this.GetCancellationTokenOnDestroy();
        rigidbody = GetComponent<Rigidbody>();

        if (rigidbody == null) { Debug.LogError(gameObject.name + ":none rb"); }

        Init();
    }


    public void Init()
    {
        #region debug用初期化処理
        UnlockInput();
        #endregion

        AttackComboReset();
        moveData.moveDis
            .Subscribe(_ => { if (!NowMoveInput) { MoveAsync().Forget(); } } )
            .AddTo(this);
    }



    #region 移動処理

    [SerializeField]
    public MoveData moveData = new MoveData(100);

    async UniTask MoveAsync()
    {
        SetNowMoveInput = true;

        while (moveData.moveDis.Value != 0)
        {
            if (CanItBeMoved || Interrupt)
            {
                NowState = CharacterState.Move;
                rigidbody.linearVelocity = ((Vector3.right * moveData.moveDis.Value) * moveData.Speed) * Time.deltaTime;
            }

            animator.SetBool("isWalk", (CanItBeMoved || Interrupt));


            await UniTask.Yield(token);
        }

        if (CanItBeMoved || Interrupt)
        {
            NowState = CharacterState.Idle;
        }

        animator.SetBool("isWalk", false);

        SetNowMoveInput = false; // 移動が完了したら移動中フラグを下ろす
    }
    #endregion

    async UniTask NonInterruptibleAction(UniTask innerTask)
    {
        Interrupt = false;
        await innerTask.AttachExternalCancellation(token);
        Interrupt = true;
    }
    #region 攻撃処理

    public void AttackInput()
    {
        if (Interrupt)
        {
            AttackCombo().Forget();
        }
    }

    async UniTask AttackCombo()
    {
        await NonInterruptibleAction(Attackasync());

        NextCombo();

        int num = NowCombo;
        float startTime = Time.time;

        Observable.EveryUpdate()
            .Where(_ => num != NowCombo || Time.time - startTime >= 2)
            .Take(1)
            .Subscribe(_ =>
            {
                if(Time.time - startTime >= 2)
                {
                    AttackComboReset();
                }
                else
                {
                    Debug.Log("time:" + (Time.time - startTime).ToString("F2"));
                }
            })
            .AddTo(this);
    }

    async UniTask Attackasync()
    {
        // 攻撃処理
        Debug.Log("Attack Combo: " + NowCombo);
        animator.SetTrigger("attack_01");
        //animator.SetTrigger("attack_0" + NowCombo);
        float startTime = Time.time;
        bool isAttack = true;

        var cancelStream = Observable.EveryUpdate()
            .Where(_ => Time.time - startTime >= 1f || !isAttack); // 終了条件を明示

        attackColl.OnTriggerEnterAsObservable()
            .TakeUntil(cancelStream) // 終了条件を監視
                    .Subscribe(col =>                           // 解除条件を満たす前だけ呼ばれる
            {
                CharacterResponseInput characterResponseInput = col.GetComponent<CharacterResponseInput>();
                if (characterResponseInput != null)
                {
                    isAttack = false; // 一度攻撃が当たったら以降の攻撃は無効化
                    characterResponseInput.DamageReaction(10f); // ここでダメージを与える
                }
            });

        await UniTask.Delay(1000, cancellationToken: token); // 攻撃アニメーションの再生時間に合わせて待機
    }

    #endregion

    public void DamageReaction()
    {
        NonInterruptibleAction(DamageReactionAsync()).Forget();
    }

    public async UniTask DamageReactionAsync()
    {

        animator.SetTrigger("DamageReact_01");

        float startTime = Time.time;
        while (Time.time - startTime < 2f)
        {
            if(Time.time - startTime < 1f)
            {

            }
            await UniTask.Yield(token);
        }

    }
    private void Update()
    {
        AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfos.Length > 0)
        {
            string clipName = clipInfos[0].clip.name;
            //Debug.Log($"Now playing clip : {clipName}");
        }
    }
}


[System.Serializable]
public class MoveData
{
    [SerializeField]
    float speed = 5f;
    public float Speed { get { return speed; } }

    public ReactiveProperty<float> moveDis { get; private set; } = new(0);

    public MoveData(float speed)
    {
        this.speed = speed;
    }
}
