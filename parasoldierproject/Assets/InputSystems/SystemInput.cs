using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SystemInput : MonoBehaviour
{

    protected const int ListSize = (int)PlayerInputNames.Set;

    protected List<string>[] MethodNames = new List<string>[ListSize];

    [SerializeField]
    private PlayerInput input = null;

    protected InputAction[] actions = new InputAction[ListSize];

    public void Init()
    {
        for (int i = 0; i < ListSize; i++)
        {
            if (Enum.IsDefined(typeof(PlayerInputNames), i))
            {
                PlayerInputNames enumValue = (PlayerInputNames)Enum.ToObject(typeof(PlayerInputNames), i);
                actions[i] = input.actions[enumValue.ToString()];
            }
        }
    }

    public void MethodSetting(Enum inputName, ActionSettype actionSettype, Action<InputAction.CallbackContext>? callPerformed = null, Action<InputAction.CallbackContext>? callCanceled = null)
    {
        int index = inputName.GetHashCode();

        if (index < 0 || index >= actions.Length) return;
        if (MethodNames[index] == null) MethodNames[index] = new List<string>();

        switch (actionSettype)
        {
            case ActionSettype.plus:
                if (callPerformed != null && !MethodNames[index].Contains(callPerformed.Method.Name))
                {
                    actions[index].performed += callPerformed;
                    MethodNames[index].Add(callPerformed.Method.Name);
                }
                if (callCanceled != null && !MethodNames[index].Contains(callCanceled.Method.Name))
                {
                    actions[index].canceled += callCanceled;
                    MethodNames[index].Add(callCanceled.Method.Name);
                }
                break;
            case ActionSettype.minus:
                if (callPerformed != null && MethodNames[index].Contains(callPerformed.Method.Name))
                {
                    actions[index].performed -= callPerformed;
                    MethodNames[index].Remove(callPerformed.Method.Name);
                }
                if (callCanceled != null && MethodNames[index].Contains(callCanceled.Method.Name))
                {
                    actions[index].canceled -= callCanceled;
                    MethodNames[index].Remove(callCanceled.Method.Name);
                }
                break;
        }
    }


    #region 入力の有効化/無効化
    /// <summary> 入力を有効化 </summary>
    public void AllOn() { input.actions.Enable(); }
    /// <summary> 入力を無効化 </summary>
    public void AllOff() { input.actions.Disable(); }

    /// <summary> 指定した入力を有効化 </summary>
    public void EnableInput(PlayerInputNames inputName)
    {
        int index = (int)inputName;
        if (index >= 0 && index < ListSize && actions[index] != null)
        {
            actions[index].Enable();
        }
    }

    /// <summary> 指定した入力を無効化 </summary>
    public void DisableInput(PlayerInputNames inputName)
    {
        int index = (int)inputName;
        if (index >= 0 && index < ListSize && actions[index] != null)
        {
            actions[index].Disable();
        }
    }

    #endregion
}
