using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputMoveAction : MonoBehaviour
{
    SystemInput systemInput;
    CharacterMove characterMove;

    private void Awake()
    {
        systemInput = GetComponent<SystemInput>();
        characterMove = GetComponent<CharacterMove>();

        if (systemInput == null || characterMove == null)
        {
            Debug.LogError("PlayerInputMoveAction requires SystemInput and CharacterMove components.");
            return;
        }
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
