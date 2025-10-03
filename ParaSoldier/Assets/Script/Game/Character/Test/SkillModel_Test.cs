using System.Threading;
using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

// テスト用のスキルモデルクラス
// キャラクターのスキル（移動、攻撃、ガードなど）の実装
public class SkillModel_Test : SkillModel_abstract
{
    // アニメーションを制御するAnimatorコンポーネント
    [SerializeField] Animator character;
    
    // 物理演算用のRigidbodyコンポーネント
    [SerializeField] Rigidbody rb;

    // 移動速度
    [SerializeField] float moveSpeed = 5f;
    
    // 移動範囲制限
    private const float MOVEMENT_LIMIT = 9.5f;

    // チャージとガードのキャンセル用トークンソース
    CancellationTokenSource chargeTokenSource;
    CancellationTokenSource guardTokenSource;

    // チャージ中とガード中の状態フラグ
    bool isCharging = false;
    bool isGuarding = false;

    // 移動入力値を監視するReactiveProperty
    ReactiveProperty<float> moveInput = new(0);

    private void Awake()
    {
        // RigidbodyとAnimatorがnullの場合、自動で取得
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("Rigidbody component not found on " + gameObject.name);
            }
        }
        
        if (character == null)
        {
            character = GetComponent<Animator>();
            if (character == null)
            {
                Debug.LogError("Animator component not found on " + gameObject.name);
            }
        }
    }
    
    private void Update()
    {
        // 毎フレーム位置制限をチェック
        ClampPosition();
    }
    
    // 位置を制限内にクランプする
    private void ClampPosition()
    {
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.x) > MOVEMENT_LIMIT)
        {
            pos.x = Mathf.Sign(pos.x) * MOVEMENT_LIMIT;
            transform.position = pos;
            
            // 速度もリセット
            if (rb != null && Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(pos.x))
            {
                Vector3 vel = rb.linearVelocity;
                vel.x = 0;
                rb.linearVelocity = vel;
            }
        }
    }

    // 移動入力を受け取るメソッド
    public override void OnMove(float x)
    {
        if (!IsInputEnabled(ActionType.Move)) return;
        
        moveInput.Value = x;
        MoveAsync(x).Forget();
    }

    // 非同期移動処理
    async UniTask MoveAsync(float x)
    {
        // Rigidbodyがnullの場合は早期リターン
        if (rb == null)
        {
            Debug.LogWarning("Rigidbody is null in MoveAsync");
            return;
        }
        
        // 現在の位置を取得
        float currentX = transform.position.x;

        // 位置が画面端を超えていたら画面端に戻す
        if (Mathf.Abs(currentX) > MOVEMENT_LIMIT)
        {
            Vector3 clampedPos = transform.position;
            clampedPos.x = Mathf.Sign(currentX) * MOVEMENT_LIMIT;
            transform.position = clampedPos;
            currentX = clampedPos.x;
        }
        
        // 移動後の位置が制限を超えるかチェック
        float nextPosX = currentX + (x * moveSpeed * Time.fixedDeltaTime);
        if (Mathf.Abs(nextPosX) >= MOVEMENT_LIMIT)
        {
            // 制限を超える場合は移動をキャンセル
            x = 0;
        }
        
        // 移動方向ベクトルの設定
        Vector3 dir = new Vector3(x, 0, 0);
        rb.linearVelocity = dir * moveSpeed;
        
        if (character != null)
        {
            character.SetBool("isWalk", x != 0);
        }

        // 移動終了まで待機（入力が0になったら停止）
        await UniTask.WaitUntil(() => moveInput.Value == 0);
        rb.linearVelocity = Vector3.zero;
        
        if (character != null)
        {
            character.SetBool("isWalk", false);
        }
    }

    // 攻撃処理
    public override void OnAttack()
    {
        if (!IsInputEnabled(ActionType.Attack)) return;
        character.SetTrigger("Attack");
        // 攻撃のコンボシステムはここに実装可能
    }

    // 水ショット処理
    public override void OnWaterShot()
    {
        if (!IsInputEnabled(ActionType.WaterShot)) return;
        character.SetTrigger("WaterShot");
    }

    // アサルト処理
    public override void OnAssault()
    {
        if (!IsInputEnabled(ActionType.Assault)) return;
        character.SetTrigger("Assault");
    }

    // チャージ開始処理
    public override void OnCharge()
    {
        if (!IsInputEnabled(ActionType.Charge)) return;
        if (!isCharging)
        {
            isCharging = true;
            chargeTokenSource = new CancellationTokenSource();
            ChargeAsync(chargeTokenSource.Token).Forget();
        }
    }

    // チャージ終了処理
    public override void OutCharge()
    {
        if (isCharging)
        {
            chargeTokenSource?.Cancel();
            isCharging = false;
        }
    }

    // 非同期チャージ処理
    async UniTask ChargeAsync(CancellationToken token)
    {
        character.SetBool("isCharge", true);
        try
        {
            // 1秒間チャージ
            await UniTask.Delay(1000, cancellationToken: token);
            Debug.Log("Charge complete! Add gage or power here.");
        }
        catch (OperationCanceledException) { }
        character.SetBool("isCharge", false);
        isCharging = false;
    }

    // ガード開始処理
    public override void OnGuard()
    {
        if (!IsInputEnabled(ActionType.Guard)) return;
        if (!isGuarding)
        {
            isGuarding = true;
            guardTokenSource = new CancellationTokenSource();
            GuardAsync(guardTokenSource.Token).Forget();
        }
    }

    // ガード終了処理
    public override void OutGuard()
    {
        if (isGuarding)
        {
            guardTokenSource?.Cancel();
            isGuarding = false;
            character.SetBool("isGuard", false);
        }
    }

    // 非同期ガード処理
    async UniTask GuardAsync(CancellationToken token)
    {
        character.SetBool("isGuard", true);
        try
        {
            await UniTask.Delay(5000, cancellationToken: token); // ガード時間上限
        }
        catch (OperationCanceledException) { }
        character.SetBool("isGuard", false);
        isGuarding = false;
    }
}