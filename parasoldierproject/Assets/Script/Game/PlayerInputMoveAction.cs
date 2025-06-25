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
}
