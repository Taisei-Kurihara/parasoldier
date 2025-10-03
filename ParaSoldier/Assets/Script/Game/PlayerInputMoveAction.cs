using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputMoveAction : CharacterStatus
{
    SystemInput systemInput;

    protected override void AwakeInit()
    {
        systemInput = GetComponent<SystemInput>();

        systemInput.Init();
        systemInput.MethodSetting(PlayerInputNames.Move, ActionSettype.plus, OnMove, OutMove);
        systemInput.MethodSetting(PlayerInputNames.Attack, ActionSettype.plus, OnAttack);
        systemInput.MethodSetting(PlayerInputNames.Assault, ActionSettype.plus, OnAssault);
        systemInput.MethodSetting(PlayerInputNames.WaterShot, ActionSettype.plus, OnWaterShot);
        systemInput.MethodSetting(PlayerInputNames.Charge, ActionSettype.plus, OnCharge);
        systemInput.MethodSetting(PlayerInputNames.Guard, ActionSettype.plus, OnGuard, OutGuard);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        characterMove.moveData.moveDis.Value = context.ReadValue<Vector2>().x;
    }

    public void OutMove(InputAction.CallbackContext context)
    {
        characterMove.moveData.moveDis.Value = 0;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        characterMove.AttackInput();
    }

    public void OnWaterShot(InputAction.CallbackContext context)
    {
        characterMove.WaterShotInput();
    }

    public void OnAssault(InputAction.CallbackContext context)
    {
        characterMove.AssaultInput();
    }

    public void OnCharge(InputAction.CallbackContext context)
    {
        Debug.Log("ccccccccccc");
        characterMove.ChargeInput();
    }

    public void OnGuard(InputAction.CallbackContext context)
    {
        characterMove.GuardInput();
    }

    public void OutGuard(InputAction.CallbackContext context)
    {
        characterMove.GuardOutInput();
    }
}
