// ==========================================
// using: 利用しているライブラリ
// ==========================================

using System;                    // C# の基本クラス (Action, IDisposable, etc.)
using System.Collections;        // コレクション用 (BitArray 等)
using System.Threading;          // CancellationToken/CancellationTokenSource 用
using Cysharp.Threading.Tasks;   // UniTask (非同期処理を Task より軽量に扱える)
using UniRx;                     // UniRx (Reactive Extensions for Unity) - ReactiveProperty など
using UniRx.Triggers;            // UniRx のトリガー拡張 (OnTriggerEnterAsObservable 等)
using UnityEngine;               // Unity の基本クラス (MonoBehaviour, Animator, Rigidbody 等)

// ==========================================
// enum: キャラクターの状態やレベル定義
// ==========================================
#region enum
public enum CharacterState
{
    Idle,              // 待機
    Move,              // 移動
    Attack_01,         // 攻撃1
    Attack_02,         // 攻撃2
    Attack_03,         // 攻撃3
    WaterShot,         // 水撃
    Charge,            // チャージ
    Assault,           // 突進
    Guard,             // 防御中
    DamageReaction,    // ダメージリアクション
    GuardTransition    // ガード移行中
}

// 攻撃の強さレベル
public enum PowerLevel { Lv0 = 0, Lv1, Lv2, Lv3, Lv4 }
// 吹き飛び耐性レベル
public enum BlowResistLevel { Lv0 = 0, Lv1, Lv2, Lv3, Lv4 }
// ダメージ軽減率レベル
public enum DamageReduceLevel { Lv0, Lv1, Lv2, Lv3, Lv4 }
// 防御状態
public enum GuardState { None, Transition, Guarding }
// 攻撃タイプ
public enum AttackType { Normal, WaterShot, Assault }
#endregion


/// <summary>
/// キャラクタの「移動・攻撃・防御・チャージ・ダメージリアクション」
/// など、モーションや行動制御を管理するコンポーネント
/// </summary>
public class CharacterMove : MonoBehaviour
{
    [SerializeField, Header("キャラクターアニメーター"), Tooltip("キャラクター本体のアニメーター")]
    Animator character;

    [SerializeField, Header("パラソルアニメーター"), Tooltip("パラソルのアニメーター")]
    Animator parasol;

    CancellationToken token; // 破棄検知用のキャンセルトークン
    Rigidbody rigidbody;     // キャラ移動用の物理ボディ


    #region モーションデータ（攻撃パターン定義）
    [SerializeField, Header("攻撃1"), Tooltip("1段目の通常攻撃データ")]
    private AttackData Attack01 = new AttackData(CharacterState.Attack_01, AttackType.Normal);

    [SerializeField, Header("攻撃2"), Tooltip("2段目の通常攻撃データ")]
    private AttackData Attack02 = new AttackData(CharacterState.Attack_02, AttackType.Normal);

    [SerializeField, Header("攻撃3"), Tooltip("3段目の通常攻撃データ")]
    private AttackData Attack03 = new AttackData(CharacterState.Attack_03, AttackType.Normal);

    [SerializeField, Header("水撃"), Tooltip("水撃攻撃データ(ゲージ1消費)")]
    private AttackData WaterShot = new AttackData(CharacterState.WaterShot, AttackType.WaterShot);

    [SerializeField, Header("突進"), Tooltip("突進攻撃データ(ゲージ3消費)")]
    private AttackData Assault = new AttackData(CharacterState.Assault, AttackType.Assault);
    #endregion


    #region WaterShot設定
    [SerializeField, Header("WaterShotパーティクルシステム"), Tooltip("水撃のパーティクルエフェクト")]
    private ParticleSystem waterShotParticle;

    [SerializeField, Header("WaterShot専用コライダー"), Tooltip("水撃の当たり判定用コライダー")]
    private Collider waterShotCollider;

    [SerializeField, Header("WaterShot発射オフセット"), Tooltip("水撃の発射位置オフセット(キャラクター位置からの相対座標)")]
    private Vector3 waterShotOffset = Vector3.zero;
    #endregion


    #region 入力管理
    // ------------------------------------------
    // 共通の入力状態管理
    // ------------------------------------------
    private CharacterState nowState = CharacterState.Idle;
    private CharacterState NowState
    {
        get => nowState;
        set
        {
            nowState = value;
            // CharacterStatus に現在の状態を反映
            GetComponent<CharacterStatus>().currentState.Value = value;
        }
    }

    // 入力制御フラグ（2bit分利用）
    BitArray generalOrder = new BitArray(2, true);

    // [0] : 入力不可 (戦闘前/終了後のロック)
    public bool inputFailure { get { return generalOrder[0]; } }
    public void UnlockInput() { generalOrder[0] = false; }
    public void DisableInput() { generalOrder[0] = true; }

    // [1] : 入力割り込み可否（モーション中に次行動が可能か）
    public bool Interrupt { get { return ((inputFailure) ? !inputFailure : generalOrder[1]); } set { generalOrder[1] = value; } }

    // 攻撃キャンセル用トークンソース
    CancellationTokenSource attackTokenSource = new();
    #endregion


    #region 移動入力管理
    BitArray moveInput = new BitArray(2, false);

    // [0] : 現在移動入力中か
    public bool NowMoveInput { get { return moveInput[0]; } }
    private bool SetNowMoveInput { set { moveInput[0] = value; } }

    // [1] : 移動可能状態か
    public bool CanItBeMoved { get { return moveInput[1]; } }
    private void UnMove() { moveInput[1] = false; }
    private void OnMove() { moveInput[1] = true; }
    #endregion


    #region 攻撃入力管理
    const int maxCombo = 3;    // 最大コンボ数
    int nowComboIndex = 0;     // 現在のコンボインデックス

    void AttackComboReset() { nowComboIndex = 0; }
    private int NowCombo { get { return (nowComboIndex + 1); } }

    void NextCombo()
    {
        nowComboIndex = (nowComboIndex + 1) % maxCombo;
    }
    #endregion

    #region 開始処理
    private void Awake()
    {
        // オブジェクト破棄時にキャンセルされるトークン
        token = this.GetCancellationTokenOnDestroy();
        rigidbody = GetComponent<Rigidbody>();

        if (rigidbody == null) { Debug.LogError(gameObject.name + ":none rb"); }

        Init();
    }

    public void Init()
    {
        // コンボリセット
        AttackComboReset();

        // 移動入力監視 (ReactiveProperty moveDis)
        moveData.moveDis
            .Subscribe(_ => { if (!NowMoveInput) { MoveAsync().Forget(); } })
            .AddTo(this);
    }
    #endregion


    #region 移動処理
    [SerializeField, Header("移動データ"), Tooltip("キャラクターの移動速度などの設定")]
    public MoveData moveData = new MoveData(100);

    /// <summary> 移動処理ループ </summary>
    async UniTask MoveAsync()
    {
        SetNowMoveInput = true;

        while (moveData.moveDis.Value != 0)
        {
            // ガード中またはダメージリアクション中は移動不能
            if (NowState == CharacterState.Guard ||
                NowState == CharacterState.GuardTransition ||
                NowState == CharacterState.DamageReaction)
            {
                character.SetBool("isWalk", false);
                await UniTask.Yield(token);
                continue;
            }

            if (CanItBeMoved || Interrupt)
            {
                NowState = CharacterState.Move;
                rigidbody.linearVelocity = (Vector3.right * moveData.moveDis.Value) * moveData.Speed;
            }

            character.SetBool("isWalk", (CanItBeMoved || Interrupt));
            await UniTask.Yield(token);
        }

        // 移動終了処理
        if (CanItBeMoved || Interrupt)
        {
            NowState = CharacterState.Idle;
        }
        character.SetBool("isWalk", false);
        SetNowMoveInput = false;
    }
    #endregion


    #region 共通メソッド
    /// <summary>
    /// 割り込み不可処理ラッパ
    /// </summary>
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

    /// <summary>
    /// 攻撃モーション再生処理
    /// </summary>
    async UniTask PlayAttackMotion(AttackData attack)
    {
        attackTokenSource?.Cancel(); // 前回攻撃キャンセル
        attackTokenSource = new CancellationTokenSource();
        CancellationToken attackToken = attackTokenSource.Token;

        NowState = attack.MoveState;
        character.SetTrigger(attack.MoveState.ToString());

        // 攻撃判定有効化
        attack.ActivateAll((col, spot) =>
        {
            CharacterStatus target = col.GetComponent<CharacterStatus>();
            if (target != null)
            {
                target.DamageReaction(spot.Damage, spot.BlowPower, spot.BlowTime, attack.AttackType);
                attack.CancelAll(); // 一度当たったら判定終了
            }
        });

        // 割り込み禁止時間
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
        // 現在のコンボ番号に応じて攻撃データ切り替え
        AttackData currentAttack = NowCombo switch
        {
            1 => Attack01,
            2 => Attack02,
            3 => Attack03,
            _ => Attack01
        };

        // 攻撃実行（割り込み不可）
        await NonInterruptActionAsync(PlayAttackMotion(currentAttack));
        NextCombo();

        // コンボ継続受付タイマー
        int num = NowCombo;
        float startTime = Time.time;

        Observable.EveryUpdate()
            .Where(_ => num != NowCombo || Time.time - startTime >= 1)
            .Take(1)
            .Subscribe(_ =>
            {
                if (Time.time - startTime >= 1.5f)
                {
                    AttackComboReset();
                }
            })
            .AddTo(this);
    }
    #endregion


    #region 水撃処理
    public void WaterShotInput()
    {
        if (Interrupt)
        {
            if (GetComponent<CharacterStatus>().CheckGage() >= 1)
            {
                GetComponent<CharacterStatus>().AddGage(-1);

                NonInterruptActionAsync(FireWaterShot()).Forget();
            }
        }
    }

    async UniTask FireWaterShot()
    {
        // WaterShotモーション再生
        NowState = CharacterState.WaterShot;
        character.SetTrigger(CharacterState.WaterShot.ToString());

        if (waterShotParticle != null)
        {
            // パーティクル発射
            waterShotParticle.Play();

            // パーティクルの設定を取得
            var main = waterShotParticle.main;
            float lifetime = main.startLifetime.constant;
            float speed = main.startSpeed.constant;

            // 当たり判定を打ち出す
            if (waterShotCollider != null)
            {
                ShootWaterShotCollider(speed, lifetime).Forget();
            }
        }

        // 割り込み禁止時間
        await UniTask.Delay((int)(WaterShot.NonInterruptTime * 1000), cancellationToken: token);
    }

    async UniTaskVoid ShootWaterShotCollider(float speed, float lifetime)
    {
        // コライダーの初期位置設定（キャラクター位置 + オフセット）
        Vector3 startPos = transform.position + waterShotOffset;
        waterShotCollider.transform.position = startPos;

        // コライダーを有効化
        waterShotCollider.enabled = true;

        bool hasHit = false;
        float elapsed = 0f;

        // ヒット判定を購読
        var hitSubscription = waterShotCollider
            .OnTriggerEnterAsObservable()
            .TakeUntilDestroy(waterShotCollider)
            .Subscribe(col =>
            {
                CharacterStatus target = col.GetComponent<CharacterStatus>();
                if (target != null)
                {
                    // WaterShotのダメージデータを使用
                    target.DamageReaction(
                        WaterShot.attackColliders[0].Damage,
                        WaterShot.attackColliders[0].BlowPower,
                        WaterShot.attackColliders[0].BlowTime,
                        AttackType.WaterShot
                    );
                    hasHit = true;
                }
            });

        // 生存期間中、コライダーを移動させる
        while (elapsed < lifetime && !hasHit)
        {
            // 右方向に移動（キャラクターの向きを考慮）
            waterShotCollider.transform.position += (Vector3.right * transform.localScale.x * speed) * Time.deltaTime;

            elapsed += Time.deltaTime;
            await UniTask.Yield(token);
        }

        // ヒット判定購読を解除
        hitSubscription?.Dispose();

        // コライダーを無効化
        waterShotCollider.enabled = false;
    }
    #endregion


    #region 突進処理
    public void AssaultInput()
    {
        if (Interrupt)
        {
            if (GetComponent<CharacterStatus>().CheckGage() >= 3)
            {
                GetComponent<CharacterStatus>().AddGage(-3);
            
                DamageReactionAsync(-1000, 0.6f).Forget();
                NonInterruptActionAsync(PlayAttackMotion(Assault)).Forget();
            }
        }
    }
    #endregion


    #region ガード処理
    CancellationTokenSource guardTokenSource;
    bool isGuarding = false;

    public void GuardInput()
    {
        if (Interrupt)
        {
            isGuarding = true;
            NonInterruptActionAsync(GuardAsync()).Forget();
        }
    }

    public void GuardOutInput()
    {
        if (isGuarding)
        {
            guardTokenSource?.Cancel();
            EndGuard();
        }
    }

    async UniTask GuardAsync()
    {
        guardTokenSource?.Cancel();
        guardTokenSource = new CancellationTokenSource();
        var guardToken = guardTokenSource.Token;

        NowState = CharacterState.GuardTransition;
        character.SetTrigger(CharacterState.GuardTransition.ToString());

        //await UniTask.Delay(300, cancellationToken: guardToken); // 展開中

        NowState = CharacterState.Guard;
        character.SetBool("isGuard", true);
        parasol.SetBool("Open", true);
        character.SetTrigger("Guard");

        // ガード中は無敵化
        SetInvincible(true);
    }

    async void EndGuard()
    {
        isGuarding = false;
        NowState = CharacterState.Idle;
        character.SetBool("isGuard", false);
        parasol.SetBool("Open", false);

        // ガード解除時に無敵解除
        SetInvincible(false);

        // ガード解除後、2フレーム間入力を受け付けない
        Interrupt = false;
        await UniTask.DelayFrame(2, cancellationToken: token);
        Interrupt = true;
    }
    #endregion


    #region チャージ処理
    CancellationTokenSource chargeTokenSource;
    bool isCharging = false;

    public void ChargeInput()
    {
        if (Interrupt && !isCharging) // ← 二重起動防止
        {
            NonInterruptActionAsync(ChargeAsync()).Forget();
            Debug.Log("Charge Start");
        }
    }

    public void ChargeOutInput()
    {
        if (isCharging)
        {
            chargeTokenSource?.Cancel();
            EndCharge();
            Debug.Log("Charge Canceled");
        }
    }

    async UniTask ChargeAsync()
    {
        chargeTokenSource?.Cancel();
        chargeTokenSource = new CancellationTokenSource();
        var chargeToken = chargeTokenSource.Token;

        NowState = CharacterState.Charge;
        isCharging = true;
        character.SetTrigger("isCharge");

        bool completed = false;
        try
        {
            await UniTask.Delay(1000, cancellationToken: chargeToken);
            completed = true;
        }
        catch (OperationCanceledException) { }

        if (completed) // ← キャンセルされていなければ加算
        {
            GetComponent<CharacterStatus>().AddGage(1);
            Debug.Log("MaxCharge! Gage Added.");
        }

        EndCharge();
    }


    void EndCharge()
    {
        isCharging = false;
        NowState = CharacterState.Idle;
    }
    #endregion


    #region ダメージリアクション処理
    // ==============================
    // 無敵状態管理
    // ==============================
    [SerializeField, Header("無敵状態"), Tooltip("現在無敵状態かどうか(デバッグ用)")]
    private bool isInvincible = false;

    [SerializeField, Header("無敵時間"), Tooltip("ダメージリアクション後の無敵時間(秒)")]
    private float invincibleTime = 1.0f;

    public bool IsInvincible => isInvincible;

    /// <summary>
    /// 無敵状態を任意に切り替える
    /// </summary>
    public void SetInvincible(bool value)
    {
        isInvincible = value;
        Debug.Log($"Invincible: {value}");
    }

    /// <summary>
    /// 一定時間だけ無敵化する
    /// </summary>
    public async UniTask InvincibleForSeconds(float seconds, CancellationToken token)
    {
        SetInvincible(true);
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);
        }
        catch (OperationCanceledException) { }
        SetInvincible(false);
    }


    // ==============================
    // ダメージリアクション処理
    // ==============================
    public void DamageReaction(float blowPower, float blowTime, AttackType attackType)
    {
        // 無敵中なら無視
        if (IsInvincible) return;

        // 各アクションを強制キャンセル
        attackTokenSource?.Cancel();
        guardTokenSource?.Cancel();
        chargeTokenSource?.Cancel();
        EndGuard();
        EndCharge();

        // 攻撃判定キャンセル
        Attack01.CancelAll();
        Attack02.CancelAll();
        Attack03.CancelAll();
        WaterShot.CancelAll();
        Assault.CancelAll();
        AttackComboReset();

        character.SetTrigger(CharacterState.DamageReaction.ToString());

        // ダメージリアクション開始
        NonInterruptActionAsync(DamageReactionAsync(blowPower, blowTime)).Forget();

        // 無敵化処理（吹き飛び時間＋α）
        InvincibleForSeconds(blowTime + invincibleTime, token).Forget();
    }

    public async UniTask DamageReactionAsync(float blowPower, float blowTime)
    {
        // DamageReaction中は入力を受け付けない
        NowState = CharacterState.DamageReaction;

        float startTime = Time.time;
        while (Time.time - startTime < blowTime + 0.1f)
        {
            if (Time.time - startTime < blowTime)
            {
                rigidbody.linearVelocity = ((Vector3.left * transform.localScale.x) * blowPower) * Time.deltaTime;
            }
            await UniTask.Yield(token);
        }

        // DamageReaction終了後、Idleに戻る
        NowState = CharacterState.Idle;
    }
    #endregion
}


// ==========================================
// 補助クラス
// ==========================================
#region まとめデータ
[System.Serializable]
public class MoveData
{
    [SerializeField, Header("移動速度"), Tooltip("キャラクターの移動速度")]
    float speed = 5f;
    public float Speed { get { return speed; } }

    // ReactiveProperty: 値変更を監視できる float
    public ReactiveProperty<float> moveDis { get; private set; } = new(0);

    public MoveData(float speed)
    {
        this.speed = speed;
    }
}
#endregion


#region モーションデータ
[System.Serializable]
public class AttackData
{
    [SerializeField, Header("判定個別管理"), Tooltip("攻撃判定のコライダーと各種パラメータの配列")]
    public HitSpot[] attackColliders;

    [SerializeField, Header("入力拒否時間"), Tooltip("攻撃モーション中に次の入力を受け付けない時間（秒）")]
    float nonInterruptTime = 0f;

    public float NonInterruptTime => nonInterruptTime;
    public CharacterState MoveState { get; private set; }
    public AttackType AttackType { get; private set; }
    private CancellationTokenSource tokenSource = new();

    public AttackData(CharacterState characterState, AttackType attackType)
    {
        this.MoveState = characterState;
        this.AttackType = attackType;
    }

    /// <summary> 全HitSpotを有効化 </summary>
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

    /// <summary> 全HitSpotをキャンセル </summary>
    public void CancelAll()
    {
        tokenSource.Cancel();
        foreach (var spot in attackColliders)
        {
            spot.ForceCancel();
        }
        tokenSource = new CancellationTokenSource();
    }
}

[System.Serializable]
public class HitSpot
{
    [SerializeField, Header("攻撃コライダー"), Tooltip("攻撃判定に使用するコライダー")]
    Collider attackCollider;

    [SerializeField, Header("発生時間"), Tooltip("攻撃判定が発生するまでの遅延時間(秒)")]
    float startDelayTime = 0.5f;

    [SerializeField, Header("持続時間"), Tooltip("攻撃判定が発生してから持続する時間(秒)")]
    float endTime = 1f;

    [SerializeField, Header("ダメージ"), Tooltip("この攻撃が与えるダメージ量")]
    float damage = 10f;
    public float Damage => damage;

    [SerializeField, Header("吹き飛び力"), Tooltip("攻撃時の吹き飛び力(大きいほど遠くへ飛ばす)")]
    float blowPower = 2000f;
    public float BlowPower => blowPower;

    [SerializeField, Header("吹き飛び時間"), Tooltip("吹き飛び効果の持続時間(秒)")]
    float blowTime = 0.5f;
    public float BlowTime => blowTime;

    private IDisposable triggerSubscription;

    /// <summary>
    /// 攻撃判定を一定時間購読し、ヒット時にコールバック実行
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

    /// <summary> 強制的に判定を終了 </summary>
    public void ForceCancel()
    {
        triggerSubscription?.Dispose();
    }
}
#endregion
