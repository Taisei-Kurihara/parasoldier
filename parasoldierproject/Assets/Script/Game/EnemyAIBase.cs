using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public enum enemyAILv
{
    Approach,         // 近寄るだけ
    ApproachAndAttack,// 近づいて攻撃
    CombatLv1         // 実戦形式 Lv1
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

            // プレイヤーが攻撃を始めたら少し遅れて距離をとる
            if (playerState.Value.ToString().StartsWith("Attack"))
            {
                float wait = UnityEngine.Random.Range(0.1f, 0.3f);
                await UniTask.Delay((int)(wait * 1000));

                // 離れる
                characterMove.moveData.moveDis.Value = 1f;

                // 攻撃終わりそうになったら近づく
                await UniTask.Delay(500);
                characterMove.moveData.moveDis.Value = -1f;
            }
            else
            {
                // プレイヤーが攻撃していない場合は近づく
                characterMove.moveData.moveDis.Value = (distance > 2f) ? -1f : 0f;
            }

            // 攻撃範囲に入っていたら攻撃
            if (distance <= 2f)
            {
                characterMove.AttackInput();
            }

            await UniTask.Delay(200);
        }
    }


}
