using System;
using Common;
using UnityEngine;
using R3;

// プレイヤー入力モデルクラス
// プレイヤーの入力を処理してキャラクターの動作に変換する
public class InputModel_Player : MonoBehaviour, InputModel_interface
{
    // コンストラクタ：キャラクタープレゼンターを設定
    public InputModel_Player(CharacterPresenter cP)
    {
        CP = cP;
    }

    // キャラクタープレゼンターへの参照
    public CharacterPresenter CP { get; set; }

    // Observable購読の解除用
    private IDisposable Dismove;
    private IDisposable DisAttack;
    private IDisposable DisWaterShot;
    private IDisposable DisAssault;
    private IDisposable DisCharge;
    private IDisposable DisGuard;
    
    // 入力設定の初期化
    public void SetUpInput()
    {
        // InputSystem_Actionsインスタンスを取得してデバッグ
        var manager = InputSystemActionsManager.Instance();
        var actions = manager.GetInputSystem_Actions();
        
        Debug.Log($"InputSystem_Actions created: {actions != null}");
        Debug.Log($"Player action map enabled before: {actions.Player.enabled}");
        
        // プレイヤー入力を有効化
        manager.PlayerEnable();
        
        Debug.Log($"Player action map enabled after: {actions.Player.enabled}");
        
        Move();
        Attack();
        WaterShot();
        Assault();
        Charge();
        Guard();
    }

    // 移動処理
    public void Move()
    {
        InputSystem_Actions action = InputSystemActionsManager.Instance().GetInputSystem_Actions();
        float beforeX = 0;  // 前フレームの入力値を保持

        // 移動処理のObservable設定
        Dismove = Observable.EveryUpdate().Subscribe(_ =>
        {
            var vec = action.Player.Move.ReadValue<Vector2>();
            if (vec.x != 0)
            {
                CP.Move(vec.x); // 入力値をプレイヤーに渡す
            }
            else if (beforeX != 0)
            {
                CP.Move(0); // 入力値が0になったらプレイヤーに0を渡す
            }

            beforeX = vec.x; // 前フレームの値を保存

        });
    }

    // 攻撃処理
    public void Attack()
    {
        InputSystem_Actions action = InputSystemActionsManager.Instance().GetInputSystem_Actions();

        DisAttack = Observable.EveryUpdate().Where(_ => action.Player.Attack.ReadValue<float>() > 0).Subscribe(_ =>
        {
            CP.OnAttack(); // 攻撃入力があったらプレイヤーに攻撃を通知
        });
    }

    // 水ショット処理
    public void WaterShot()
    {
        InputSystem_Actions action = InputSystemActionsManager.Instance().GetInputSystem_Actions();

        DisWaterShot = Observable.EveryUpdate().Where(_ => action.Player.WaterShot.ReadValue<float>() > 0).Subscribe(_ =>
        {
            CP.OnWaterShot(); // 水ショット入力があったらプレイヤーに水ショットを通知
        });
    }

    // アサルト処理
    public void Assault()
    {
        InputSystem_Actions action = InputSystemActionsManager.Instance().GetInputSystem_Actions();
        DisAssault = Observable.EveryUpdate().Where(_ => action.Player.Assault.ReadValue<float>() > 0).Subscribe(_ =>
        {
            CP.OnAssault(); // アサルト入力があったらプレイヤーにアサルトを通知
        });
    }

    // チャージ処理
    public void Charge()
    {
        InputSystem_Actions action = InputSystemActionsManager.Instance().GetInputSystem_Actions();
        bool beforeCharge = false; // 前フレームのチャージ状態を保持する変数
        DisCharge = Observable.EveryUpdate().Subscribe(_ =>
        {
            bool isCharging = action.Player.Charge.ReadValue<float>() > 0;
            if (isCharging && !beforeCharge)
            {
                CP.OnCharge(); // チャージ入力があったらプレイヤーにチャージを通知
            }
            else if (!isCharging && beforeCharge)
            {
                CP.OutCharge(); // チャージ入力がなくなったらチャージ解除
            }
            beforeCharge = isCharging; // 前フレームのチャージ状態を保存
        });
    }

    // ガード処理
    public void Guard()
    {
        InputSystem_Actions action = InputSystemActionsManager.Instance().GetInputSystem_Actions();
        bool beforeGuard = false; // 前フレームのガード状態を保持する変数
        DisGuard = Observable.EveryUpdate().Subscribe(_ =>
        {
            bool isGuarding = action.Player.Guard.ReadValue<float>() > 0;
            if (isGuarding && !beforeGuard)
            {
                CP.OnGuard(); // ガード入力があったらプレイヤーにガードを通知
            }
            else if (!isGuarding && beforeGuard)
            {
                CP.OutGuard(); // ガード入力がなくなったらガード解除
            }
            beforeGuard = isGuarding; // 前フレームのガード状態を保存
        });
    }

    // リソースの解放処理
    public void Dispose()
    {
        Dismove?.Dispose();
        DisAttack?.Dispose();
        DisWaterShot?.Dispose();
        DisAssault?.Dispose();
        DisCharge?.Dispose();
        DisGuard?.Dispose();
    }

}