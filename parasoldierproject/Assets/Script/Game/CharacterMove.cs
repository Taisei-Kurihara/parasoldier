using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

# region enum
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
    DamageReaction,
    GuardTransition

}

public enum PowerLevel { Lv0 = 0, Lv1 = 1, Lv2 = 2, Lv3 = 3, Lv4 = 4 } // 攻撃の強さ
public enum BlowResistLevel { Lv0 = 0, Lv1 = 1, Lv2 = 2, Lv3 = 3, Lv4 = 4 } // 吹き飛び耐性
public enum DamageReduceLevel { Lv0, Lv1, Lv2, Lv3, Lv4 } // ダメージ軽減率
public enum GuardState { None, Transition, Guarding } // 防御状態
#endregion

/// <summary> キャラクタの動きだけを管理する </summary>
public class CharacterMove : MonoBehaviour
{
    [SerializeField]
    Animator animator;

    CancellationToken token;
    Rigidbody rigidbody;

    #region モーションデータ
    [SerializeField] private AttackData Attack01 = new AttackData(CharacterState.Attack_01);
    [SerializeField] private AttackData Attack02 = new AttackData(CharacterState.Attack_02);
    [SerializeField] private AttackData Attack03 = new AttackData(CharacterState.Attack_03);
    [SerializeField] private AttackData WaterShot = new AttackData(CharacterState.WaterShot);
    [SerializeField] private AttackData Assault = new AttackData(CharacterState.Assault);
    #endregion


    #region 入力管理

    #region 全体入力管理
    private CharacterState nowState = CharacterState.Idle;
    private CharacterState NowState
    {
        get => nowState;
        set
        {
            nowState = value;
            GetComponent<CharacterStatus>().currentState.Value = value;
        }
    }


    BitArray generalOrder = new BitArray(2, true);

    /// <summary> 戦闘前or決着後に動けなくする用 </summary>
    public bool inputFailure { get { return generalOrder[0]; } }
    public void UnlockInput() { generalOrder[0] = false; }
    public void DisableInput() { generalOrder[0] = true; }

    // / <summary> 入力割り込みできるか </summary>
    public bool Interrupt { get { return (inputFailure) ? !inputFailure:generalOrder[1]; } set { generalOrder[1] = value; } }
    
    CancellationTokenSource attackTokenSource = new(); // 攻撃中断用

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

    #region 開始処理
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
        //UnlockInput();
        #endregion

        AttackComboReset();
        moveData.moveDis
            .Subscribe(_ => { if (!NowMoveInput) { MoveAsync().Forget(); } } )
            .AddTo(this);
    }

    #endregion

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


    #region 共通メソッド
    void NonInterruptCheck(UniTask innerTask)
    {
        if (Interrupt)
        {
            NonInterruptActionAsync(innerTask).Forget();
        }
    }

    async UniTask NonInterruptActionAsync(UniTask innerTask)
    {
        Interrupt = false;
        await innerTask.AttachExternalCancellation(token);
        Interrupt = true;
    }


    async UniTask PlayAttackMotion(AttackData attack)
    {
        attackTokenSource?.Cancel(); // 前回攻撃中断
        attackTokenSource = new CancellationTokenSource();
        CancellationToken attackToken = attackTokenSource.Token;

        NowState = attack.MoveState;

        animator.SetTrigger(attack.MoveState.ToString());

        attack.ActivateAll((col, spot) =>
        {
            //Debug.Log("hit:" + col.name);
            CharacterStatus characterResponseInput = col.GetComponent<CharacterStatus>();
            if (characterResponseInput != null)
            {
                characterResponseInput.DamageReaction(spot.Damage, spot.BlowPower, spot.BlowTime);
                attack.CancelAll();
            }
        });

        await UniTask.Delay((int)(attack.NonInterruptTime * 1000), cancellationToken: attackToken);

    }
    #endregion


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
        AttackData currentAttack = NowCombo switch
        {
            1 => Attack01,
            2 => Attack02,
            3 => Attack03,
            _ => Attack01
        };

        await NonInterruptActionAsync(PlayAttackMotion(currentAttack));

        NextCombo();

        #region コンボタイマー
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
                    //Debug.Log("time:" + (Time.time - startTime).ToString("F2"));
                }
            })
            .AddTo(this);
        #endregion
    }



    #endregion

    #region 水撃処理
    public void WaterShotInput()
    {
        NonInterruptCheck(PlayAttackMotion(WaterShot));
        
    }
    #endregion

    #region 突進処理
    public void AssaultInput()
    {
        NonInterruptCheck(PlayAttackMotion(Assault));
    }
    #endregion


    #region ガード処理
    CancellationTokenSource guardTokenSource;
    bool isGuarding = false;

    public void GuardInput()
    {
        NonInterruptCheck(GuardAsync());
    }

    public void GuardOutInput()
    {
        if (isGuarding)
        {
            guardTokenSource?.Cancel(); // 終了処理
            EndGuard();
        }
    }

    async UniTask GuardAsync()
    {
        guardTokenSource?.Cancel();
        guardTokenSource = new CancellationTokenSource();
        var guardToken = guardTokenSource.Token;

        NowState = CharacterState.GuardTransition;
        animator.SetTrigger(CharacterState.GuardTransition.ToString());

        await UniTask.Delay(300, cancellationToken: guardToken); // 展開中

        NowState = CharacterState.Guard;
        isGuarding = true;
        animator.SetBool("isGuard", true);
    }

    void EndGuard()
    {
        isGuarding = false;
        NowState = CharacterState.Idle;
        animator.SetBool("isGuard", false);
    }

    #endregion

    #region チャージ処理

    CancellationTokenSource chargeTokenSource;
    bool isCharging = false;

    public void ChargeInput()
    {
        NonInterruptCheck(ChargeAsync());
    }

    public void ChargeOutInput()
    {
        if (isCharging)
        {
            chargeTokenSource?.Cancel();
            EndCharge();
        }
    }

    async UniTask ChargeAsync()
    {
        chargeTokenSource?.Cancel();
        chargeTokenSource = new CancellationTokenSource();
        var chargeToken = chargeTokenSource.Token;

        NowState = CharacterState.Charge;
        isCharging = true;
        animator.SetBool("isCharge", true);
        await UniTask.Delay(2000, cancellationToken: chargeToken);
        Debug.Log("MaxCharge!");
    }

    void EndCharge()
    {
        isCharging = false;
        NowState = CharacterState.Idle;
        animator.SetBool("isCharge", false);
    }

    #endregion



    #region ダメージリアクション処理
    public void DamageReaction(float blowPower, float blowTime)
    {
        // 攻撃やその他アクション強制キャンセ
        attackTokenSource?.Cancel();
        guardTokenSource?.Cancel();
        chargeTokenSource?.Cancel();
        EndGuard();
        EndCharge();

        // HitSpotやAttackDataキャンセル
        Attack01.CancelAll();
        Attack02.CancelAll();
        Attack03.CancelAll();
        WaterShot.CancelAll();
        Assault.CancelAll();

        AttackComboReset();

        NonInterruptActionAsync(DamageReactionAsync(blowPower, blowTime)).Forget();
    }



    public async UniTask DamageReactionAsync(float blowPower, float blowTime)
    {
        animator.SetTrigger(CharacterState.DamageReaction.ToString());

        float startTime = Time.time;
        while (Time.time - startTime < blowTime + 0.1f)
        {
            if (Time.time - startTime < blowTime)
            {
                rigidbody.linearVelocity = ((Vector3.left * transform.localScale.x) * blowPower) * Time.deltaTime;
            }
            await UniTask.Yield(token);
        }
    }


    #endregion
}

#region まとめデータ
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
#endregion


# region モーションデータ

[System.Serializable]
public class AttackData
{
    [SerializeField,Header("判定個別管理")]
    HitSpot[] attackColliders;

    [SerializeField,Header("入力拒否時間")]
    float nonInterruptTime = 0f;

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

    [SerializeField]
    float blowTime = 0.5f; // 追加
    public float BlowTime => blowTime; // 追加

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

# endregion
