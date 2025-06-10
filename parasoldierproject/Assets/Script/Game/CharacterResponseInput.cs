using UnityEngine;

public abstract class CharacterResponseInput : MonoBehaviour
{
    CharacterMove characterMove;

    private void Awake()
    {
        characterMove = GetComponent<CharacterMove>();
        AwakeInit();
    }

    protected abstract void AwakeInit();
}
