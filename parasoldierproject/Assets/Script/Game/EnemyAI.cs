using Unity.VisualScripting;
using UnityEngine;

public enum enemyAILv
{
    Approach, // ‹ßŠñ‚é
}

public class EnemyAI : MonoBehaviour
{
    [SerializeField]
    SelectStage identityStage = SelectStage.TestStage;
    public SelectStage IdentityStage { get { return identityStage; } }

    CharacterMove characterMove;

    private void Awake()
    {
        characterMove = GetComponent<CharacterMove>();
    }



}
