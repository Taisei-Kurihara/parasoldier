using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public enum CharacterState
{
    Idle,
    Move,
    Attack_01,
    Attack_02,
    Attack_03,
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

    [SerializeField] private AttackData attack01;
    [SerializeField] private AttackData attack02;
    [SerializeField] private AttackData attack03;
    [SerializeField] private AttackData waterShot;
    [SerializeField] private AttackData assault;

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
        Debug.Log("Attack Combo: " + NowCombo);

        AttackData currentAttack = NowCombo switch
        {
            1 => attack01,
            2 => attack02,
            3 => attack03,
            _ => attack01
        };

        animator.SetTrigger(currentAttack.MoveState.ToString());

        currentAttack.ActivateAll((col, spot) =>
        {
            CharacterStatus characterResponseInput = col.GetComponent<CharacterStatus>();
            if (characterResponseInput != null)
            {
                float damage = spot.Damage;
                float blowPower = spot.BlowPower;
                characterResponseInput.DamageReaction(damage);
            }
        });

        await UniTask.Delay((int)(currentAttack.NonInterruptTime * 1000), cancellationToken: token);
    }




    void Attackcolls(Collider[] attackColliders)
    {
        ReactiveProperty<Collider> hitTarget = new ReactiveProperty<Collider>(null);
        float startTime = Time.time;
        bool isAttack = true;

        // 終了トリガーの監視（1秒経過 or 攻撃済み）
        var cancelStream = Observable.EveryUpdate()
            .Where(_ => Time.time - startTime >= 1f || !isAttack);

        foreach (var c in attackColliders)
        {
            c.OnTriggerEnterAsObservable()
            .TakeUntil(cancelStream) // 終了条件を監視
                    .Subscribe(col =>                           // 解除条件を満たす前だけ呼ばれる
                    {
                        CharacterStatus characterResponseInput = col.GetComponent<CharacterStatus>();
                        if (characterResponseInput != null)
                        {
                            isAttack = false; // 一度攻撃が当たったら以降の攻撃は無効化
                            hitTarget.Value = col; // ヒットしたターゲットを設定
                        }
                    });
        }

        // 1回だけリアクションしたいとき
        hitTarget
            .Where(col => col != null)
            .Take(1)
            .Subscribe(col =>
            {
                Debug.Log($"HitTarget: {col.name}");
                // ここでゲームロジックを反応させる（例：攻撃UIやエフェクト）
            });
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
        while (Time.time - startTime < 0.6f)
        {
            if(Time.time - startTime < 0.5f)
            {
                rigidbody.linearVelocity = ((Vector3.left * transform.localScale.x) * 2000) * Time.deltaTime;
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


[System.Serializable]
public class AttackData
{
    [SerializeField]
    HitSpot[] attackColliders;

    [SerializeField]
    float nonInterruptTime = 1f;

    public float NonInterruptTime => nonInterruptTime;

    public CharacterState MoveState { get; private set; }

    private CancellationTokenSource tokenSource = new();

    public AttackData(CharacterState characterState)
    {
        this.MoveState = characterState;
    }

    /// <summary>
    /// 全HitSpotを有効化（OnTrigger購読開始）
    /// </summary>
    public void ActivateAll(Action<Collider, HitSpot> onHitCallback)
    {
        foreach (var spot in attackColliders)
        {
            spot.TriggerSubscribe(tokenSource.Token, (col) =>
            {
                onHitCallback(col, spot);
                CancelAll(); // どれか当たった時点で全体キャンセル
            }).Forget();
        }
    }


    /// <summary>
    /// 全HitSpotの処理をキャンセル
    /// </summary>
    public void CancelAll()
    {
        tokenSource.Cancel();
        foreach (var spot in attackColliders)
        {
            spot.ForceCancel();
        }
        tokenSource = new CancellationTokenSource(); // 再利用できるよう新規生成
    }
}

[System.Serializable]
public class HitSpot
{
    [SerializeField]
    Collider attackCollider;

    [SerializeField]
    float startDelayTime = 0.5f;

    [SerializeField]
    float endTime = 1f;

    [SerializeField]
    float damage = 10f;
    public float Damage => damage;

    [SerializeField]
    float blowPower = 2000f;
    public float BlowPower => blowPower;

    private IDisposable triggerSubscription;

    /// <summary>
    /// トリガーイベントを購読し、一定時間後に解除
    /// </summary>
    public async UniTaskVoid TriggerSubscribe(CancellationToken token, Action<Collider> onHitCallback)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(startDelayTime), cancellationToken: token);

            triggerSubscription = attackCollider
                .OnTriggerEnterAsObservable()
                .TakeUntilDestroy(attackCollider)
                .TakeUntil(UniTask.Delay(TimeSpan.FromSeconds(endTime), cancellationToken: token).ToObservable())
                .Subscribe(onHitCallback);
        }
        catch (OperationCanceledException)
        {
            // キャンセル時は何もしない
        }
    }


    /// <summary>
    /// 外部からの強制キャンセル処理
    /// </summary>
    public void ForceCancel()
    {
        triggerSubscription?.Dispose();
    }
}
