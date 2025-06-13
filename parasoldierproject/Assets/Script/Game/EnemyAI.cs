using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public enum enemyAILv
{
    Approach, // ãﬂäÒÇÈ
}

public class EnemyAI : CharacterResponseInput
{
    [SerializeField]
    SelectStage identityStage = SelectStage.TestStage;

    enemyAILv aiLevel = enemyAILv.Approach;
    enemyAILv AiLevel { get { return aiLevel; } set { aiLevel = value; } }

    public SelectStage IdentityStage { get { return identityStage; } }

    protected override void AwakeInit()
    {
        // Ç±Ç±Ç≈AIÇÃèâä˙âªÇçsÇ§
        switch (aiLevel)
        {
            case enemyAILv.Approach:
                Approach().Forget();
                break;
        }
    }

    async UniTask Approach()
    {
        characterMove.moveData.moveDis.Value = 1;
    }



}
