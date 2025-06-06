using Unity.VisualScripting;
using UnityEngine;

public enum enemyAILv
{
    Approach, // ‹ßŠñ‚é
}

public class EnemyAI : MonoBehaviour
{
    CharacterMove characterMove;

    private void Awake()
    {
        characterMove = GetComponent<CharacterMove>();
    }



}
