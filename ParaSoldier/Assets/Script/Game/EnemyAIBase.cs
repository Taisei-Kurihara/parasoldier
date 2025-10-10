using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public enum enemyAILv
{
    Approach,         // 接近するだけ
    ApproachAndAttack,// 近づいて攻撃
    CombatLv1,        // 戦闘パターン Lv1
    CombatLv2         // 戦闘パターン Lv2 (フェイント、カウンター、様子見)
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
            case enemyAILv.CombatLv2:
                CombatLv2().Forget();
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
        bool isEvading = false; // 回避中フラグ
        bool attackDetected = false; // 攻撃検知フラグ
        float attackDetectedTime = 0f; // 攻撃検知時刻
        float evadeStartDelay = 0f; // 回避開始までの遅延時間

        while (true)
        {
            float distance = Vector3.Distance(transform.position, GameManager.Instance.PlayerTransform.position);

            // プレイヤーが攻撃を始めた瞬間を検知
            if (playerState.Value.ToString().StartsWith("Attack") && !attackDetected && !isEvading && distance <= 3f)
            {
                attackDetected = true;
                attackDetectedTime = Time.time;

                // 回避開始までの遅延時間を設定
                float baseframe = 1f / 60f;
                float min = baseframe * 10;
                float max = baseframe * 30;
                evadeStartDelay = UnityEngine.Random.Range(min, max);
            }

            // 攻撃検知フラグが立っている && 遅延時間経過 => 回避開始
            if (attackDetected && Time.time >= attackDetectedTime + evadeStartDelay)
            {
                isEvading = true;
                attackDetected = false; // フラグを折る

                // 後退
                characterMove.moveData.moveDis.Value = 1f;
                await UniTask.Delay(400);

                // 移動停止
                characterMove.moveData.moveDis.Value = 0f;

                // 攻撃終了待機
                await UniTask.WaitUntil(() => !playerState.Value.ToString().StartsWith("Attack"));

                isEvading = false;
            }
            else if (!isEvading && !attackDetected)
            {
                // 通常行動: 距離に応じて接近または攻撃
                if (distance > 2f)
                {
                    // 接近
                    characterMove.moveData.moveDis.Value = -1f;
                }
                else if (distance <= 2f)
                {
                    // 攻撃範囲内なら停止して攻撃
                    characterMove.moveData.moveDis.Value = 0f;
                    characterMove.AttackInput();
                    await UniTask.Delay(500); // 攻撃後の硬直時間
                }
            }

            await UniTask.Delay(100);
        }
    }

    async UniTask CombatLv2()
    {
        var playerStatus = GameManager.Instance.PlayerTransform.GetComponent<CharacterStatus>();
        var playerState = playerStatus.currentState;
        bool isEvading = false;
        bool isCounterWaiting = false;
        float lastActionTime = 0f;

        while (true)
        {
            float distance = Vector3.Distance(transform.position, GameManager.Instance.PlayerTransform.position);
            float currentTime = Time.time;

            // プレイヤーの攻撃に対する反応
            if (playerState.Value.ToString().StartsWith("Attack") && !isEvading && !isCounterWaiting && distance <= 3.5f)
            {
                // ランダムで回避またはカウンター待機を選択
                float reaction = UnityEngine.Random.value;

                if (reaction < 0.6f) // 60%で回避
                {
                    isEvading = true;

                    float wait = UnityEngine.Random.Range(0.05f, 0.2f);
                    await UniTask.Delay((int)(wait * 1000));

                    // 後退
                    characterMove.moveData.moveDis.Value = 1f;
                    await UniTask.Delay(350);

                    characterMove.moveData.moveDis.Value = 0f;
                    await UniTask.WaitUntil(() => !playerState.Value.ToString().StartsWith("Attack"));

                    isEvading = false;
                }
                else // 40%でカウンター待機
                {
                    isCounterWaiting = true;

                    characterMove.moveData.moveDis.Value = 0f; // 停止
                    await UniTask.WaitUntil(() => !playerState.Value.ToString().StartsWith("Attack"));

                    // カウンター攻撃
                    if (distance <= 2.5f)
                    {
                        characterMove.AttackInput();
                        await UniTask.Delay(600);
                    }

                    isCounterWaiting = false;
                }

                lastActionTime = currentTime;
            }
            else if (!isEvading && !isCounterWaiting)
            {
                // 通常行動
                if (distance > 3f)
                {
                    // 遠距離: 接近
                    characterMove.moveData.moveDis.Value = -1f;
                }
                else if (distance > 2f && distance <= 3f)
                {
                    // 中距離: フェイント動作（前後移動）
                    if (currentTime - lastActionTime > 1.5f)
                    {
                        float feint = UnityEngine.Random.value;
                        if (feint < 0.3f) // 30%でフェイント
                        {
                            // 接近してすぐ後退
                            characterMove.moveData.moveDis.Value = -1f;
                            await UniTask.Delay(200);
                            characterMove.moveData.moveDis.Value = 1f;
                            await UniTask.Delay(150);
                            characterMove.moveData.moveDis.Value = 0f;

                            lastActionTime = currentTime;
                        }
                        else
                        {
                            characterMove.moveData.moveDis.Value = -1f;
                        }
                    }
                    else
                    {
                        characterMove.moveData.moveDis.Value = -1f;
                    }
                }
                else if (distance <= 2f)
                {
                    // 近距離: 攻撃
                    characterMove.moveData.moveDis.Value = 0f;

                    // 様子見（20%の確率で少し待つ）
                    if (UnityEngine.Random.value < 0.2f && currentTime - lastActionTime > 2f)
                    {
                        await UniTask.Delay(UnityEngine.Random.Range(300, 600));
                    }

                    characterMove.AttackInput();
                    await UniTask.Delay(500);
                    lastActionTime = currentTime;
                }
            }

            await UniTask.Delay(80);
        }
    }


}
