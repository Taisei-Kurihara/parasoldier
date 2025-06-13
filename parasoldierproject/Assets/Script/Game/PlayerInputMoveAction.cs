using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputMoveAction : CharacterResponseInput
{
    SystemInput systemInput;

    protected override void AwakeInit()
    {

        systemInput = GetComponent<SystemInput>();

        systemInput.Init();
        systemInput.MethodSetting(PlayerInputNames.Move, ActionSettype.plus, OnMove, OutMove);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        characterMove.moveData.moveDis.Value = context.ReadValue<Vector2>().x;
    }

    public void OutMove(InputAction.CallbackContext context)
    {
        characterMove.moveData.moveDis.Value = 0;
    }
}
