using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public enum enemyAILv
{
    Approach,         // ‹ßŠñ‚é‚¾‚¯
    ApproachAndAttack,// ‹ß‚Ã‚¢‚ÄUŒ‚
    CombatLv1         // ÀíŒ`® Lv1
}


public class EnemyAIBase : CharacterStatus
{
    [SerializeField]
    SelectStage identityStage = SelectStage.TestStage;

    enemyAILv aiLevel = enemyAILv.Approach;
    enemyAILv AiLevel { get { return aiLevel; } set { aiLevel = value; } }
    public void SetAiLevel(enemyAILv level)
    {
        aiLevel = level;
    }

    public SelectStage IdentityStage { get { return identityStage; } }

    protected override void AwakeInit()
    {

    }

    public void Lv()
    {
        switch (aiLevel)
        {
            case enemyAILv.ApproachAndAttack:
                ApproachAndAttack().Forget();
                break;
            case enemyAILv.CombatLv1:
                CombatLv1().Forget();
                break;
            default:
                Approach().Forget();
                break;
        }
    }


    async UniTask Approach()
    {
        while (true)
        {
            characterMove.moveData.moveDis.Value = -1f;
            await UniTask.Yield();
        }
    }

    async UniTask ApproachAndAttack()
    {
        var target = GameManager.Instance.PlayerTransform;
        while (true)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            characterMove.moveData.moveDis.Value = (distance > 2f) ? -1f : 0f;

            if (distance <= 2f)
            {
                characterMove.AttackInput();
            }

            await UniTask.Delay(200);
        }
    }

    async UniTask CombatLv1()
    {
        var playerStatus = GameManager.Instance.PlayerTransform.GetComponent<CharacterStatus>();
        var playerState = playerStatus.currentState;

        while (true)
        {
            float distance = Vector3.Distance(transform.position, GameManager.Instance.PlayerTransform.position);

            // ƒvƒŒƒCƒ„[‚ªUŒ‚‚ğn‚ß‚½‚ç­‚µ’x‚ê‚Ä‹——£‚ğ‚Æ‚é
            if (playerState.Value.ToString().StartsWith("Attack"))
            {
                float wait = UnityEngine.Random.Range(0.1f, 0.3f);
                await UniTask.Delay((int)(wait * 1000));

                // —£‚ê‚é
                characterMove.moveData.moveDis.Value = 1f;

                // UŒ‚I‚í‚è‚»‚¤‚É‚È‚Á‚½‚ç‹ß‚Ã‚­
                await UniTask.Delay(500);
                characterMove.moveData.moveDis.Value = -1f;
            }

            // UŒ‚”ÍˆÍ‚É“ü‚Á‚Ä‚¢‚½‚çUŒ‚
            if (distance <= 2f)
            {
                characterMove.AttackInput();
            }

            await UniTask.Delay(200);
        }
    }


}
