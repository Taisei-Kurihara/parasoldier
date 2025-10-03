using UnityEngine;
using System;
using System.Collections.Generic;

// アクションの状態を示すenum
public enum ActionState
{
    Idle,           // 完全終了
    InputReceived,  // 入力された
    PreMotion,      // 予備動作実行中
    MainMotion,     // 本動作実行中
    Recovery        // 後隙
}

// アクションタイプ
[Flags]
public enum ActionType
{
    None = 0,
    Move = 1 << 0,
    Attack = 1 << 1,
    WaterShot = 1 << 2,
    Assault = 1 << 3,
    Charge = 1 << 4,
    Guard = 1 << 5,
    All = Move | Attack | WaterShot | Assault | Charge | Guard
}

public abstract class SkillModel_abstract : MonoBehaviour
{
    // 入力制御用のフラグ
    protected ActionType enabledInputs = ActionType.All;
    
    // 各アクションの状態
    protected Dictionary<ActionType, ActionState> actionStates = new Dictionary<ActionType, ActionState>
    {
        { ActionType.Move, ActionState.Idle },
        { ActionType.Attack, ActionState.Idle },
        { ActionType.WaterShot, ActionState.Idle },
        { ActionType.Assault, ActionState.Idle },
        { ActionType.Charge, ActionState.Idle },
        { ActionType.Guard, ActionState.Idle }
    };

    // 移動処理
    public abstract void OnMove(float x);

    // 攻撃関連
    public abstract void OnAttack();
    public abstract void OnWaterShot();
    public abstract void OnAssault();

    // チャージ
    public abstract void OnCharge();
    public abstract void OutCharge();

    // ガード
    public abstract void OnGuard();
    public abstract void OutGuard();
    
    #region 入力制御（virtual実装）
    
    // 全ての入力を無効化
    public virtual void DisableAllInputs()
    {
        enabledInputs = ActionType.None;
    }
    
    // 全ての入力を有効化
    public virtual void EnableAllInputs()
    {
        enabledInputs = ActionType.All;
    }
    
    // 指定したものを除き全ての入力を無効化
    public virtual void DisableAllInputsExcept(ActionType exceptions)
    {
        enabledInputs = exceptions;
    }
    
    // 指定したものを除き全ての入力を有効化
    public virtual void EnableAllInputsExcept(ActionType exceptions)
    {
        enabledInputs = ActionType.All & ~exceptions;
    }
    
    // 特定の入力が有効かチェック
    public virtual bool IsInputEnabled(ActionType action)
    {
        return (enabledInputs & action) != 0;
    }
    
    // アクションの状態を取得
    public virtual ActionState GetActionState(ActionType action)
    {
        if (actionStates.TryGetValue(action, out ActionState state))
        {
            return state;
        }
        return ActionState.Idle;
    }
    
    // アクションの状態を設定（継承クラス用protected）
    protected virtual void SetActionState(ActionType action, ActionState state)
    {
        if (actionStates.ContainsKey(action))
        {
            actionStates[action] = state;
        }
    }
    
    #endregion
}