using System;
using System.Collections.Generic;
using UnityEngine;

public enum InputNames
{
    PlayerInputNames,
    UIInputNames
}

public enum PlayerInputNames
{
    Set = 32,
    Move = 0,
    Attack = 1,
    WaterShot = 2,
    Charge = 3,
    Assault = 4,
    Guard = 5
}

public enum ActionSettype
{
    plus,
    minus,
}

public abstract class InputSetList :MonoBehaviour
{
    protected const int ListSize = (int)PlayerInputNames.Set;

    protected List<string>[] MethodNames = new List<string>[ListSize];

    public abstract void Init();
    public abstract void MethodSetting(PlayerInputNames inputName, ActionSettype actionSettype, Action? callPerformed, Action? callCanceled);

    #region 入力の有効化/無効化
    /// <summary> 全入力を有効化 </summary>
    public abstract void AllOn();
    /// <summary> 全入力を無効化 </summary>
    public abstract void AllOff();

    /// <summary> 指定した入力を有効化 </summary>
    public abstract void EnableInput(PlayerInputNames inputName);

    /// <summary> 指定した入力を無効化 </summary>
    public abstract void DisableInput(PlayerInputNames inputName);

    #endregion
}
