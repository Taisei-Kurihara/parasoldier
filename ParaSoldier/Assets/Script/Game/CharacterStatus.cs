using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

public abstract class CharacterStatus : MonoBehaviour
{
    protected CharacterMove characterMove;

    private void Awake()
    {
        characterMove = GetComponent<CharacterMove>();
        AwakeInit();
    }

    protected abstract void AwakeInit();

    [HideInInspector]
    public ReactiveProperty<float> hp = new ReactiveProperty<float>(100);

    // 最大ゲージ数
    const int MaxGageLevel = 5;

    // 内部のゲージ値（0〜100）
    [HideInInspector]
    public ReactiveProperty<float> gage = new ReactiveProperty<float>(0);

    // 現在の段階ゲージ（0〜5）
    [HideInInspector]
    public ReactiveProperty<int> gageLevel = new ReactiveProperty<int>(0);

    [HideInInspector]
    public ReactiveProperty<CharacterState> currentState = new(CharacterState.Idle);

    /// <summary> このキャラクターがP1かP2かを示す </summary>
    [HideInInspector]
    public PType OwnerType { get; set; }

    public void DamageReaction(float damage, float blowPower, float blowTime, AttackType attackType)
    {
        // CharacterMoveの無敵状態を確認(ガード無敵も含む).
        if (characterMove != null && characterMove.IsInvincible)
        {
            Debug.Log($"Invincible! Attack blocked: {attackType}");
            // ガード中でWaterShotの場合はゲージを加算.
            if (currentState.Value == CharacterState.Guard && attackType == AttackType.WaterShot)
            {
                AddGage(1);
            }
            else
            {
                characterMove.OnGuardHit(blowPower, blowTime, attackType);
            }
            return;
        }

        damage = (attackType == AttackType.Assault) ? damage * 1.5f : damage;
        blowPower = (attackType == AttackType.Assault) ? blowPower * 1.5f : blowPower;

        Debug.Log($"Damage received from: {attackType}");
        hp.Value -= damage;
        if(hp.Value <= 0)
        {
            // (既)修:倒れる処理が0.2秒程度継続されるようにする
            // 倒れる処理を非同期で実行.
            FallDownAsync().Forget();
        }
        characterMove.DamageReaction(blowPower, blowTime, attackType);
    }

    /// <summary> キャラクターが倒れる処理を0.2秒間実行する. </summary>
    private async UniTask FallDownAsync()
    {
        characterMove.rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

        // キャラクターのコライダーを無効化して、他のキャラクターとの衝突を無視.

        // 相手のTransformを取得.
        Transform opponentTransform = (OwnerType == PType.P1) ? GameManager.Instance.EnemyTransform : GameManager.Instance.PlayerTransform;

        if (opponentTransform != null)
        {
            // 相手の方向に向かって倒れるように、Y軸制約を解除してトルクを加える.
            characterMove.rigidbody.constraints = RigidbodyConstraints.FreezeRotationX;

            // 相手の方向を計算して、その方向にトルクを加える.
            Vector3 directionToOpponent = (opponentTransform.position - transform.position).normalized;
            float torqueDirection = (directionToOpponent.x > 0) ? -1f : 1f;
            characterMove.rigidbody.AddTorque(Vector3.forward * torqueDirection * 300f, ForceMode.Impulse);
        }
        else
        {
            // 相手が見つからない場合はデフォルトの挙動.
            characterMove.rigidbody.AddTorque(Vector3.forward * 100f, ForceMode.Impulse);
        }

        // 倒れる処理を0.2秒間継続.
        await UniTask.Delay(System.TimeSpan.FromSeconds(0.2f), cancellationToken: this.GetCancellationTokenOnDestroy());

        // 0.2秒後にY軸回転を再度固定.
        characterMove.rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
    }

    public void AddGage(int amount)
    {

        // 現在の段階を更新（0〜5）
        gageLevel.Value = Mathf.Clamp((gageLevel.Value + amount), 0, MaxGageLevel+1);

        // 内部の%ゲージを増加
        gage.Value = (float)gageLevel.Value / (float)MaxGageLevel;

        Debug.Log($"gageLevel.Value:{gageLevel.Value},gage.Value:{gage.Value}");
    }

    /// <summary>
    /// 今の段階ゲージ本数を返す (0〜5)
    /// </summary>
    public int CheckGage()
    {
        return gageLevel.Value;
    }
}
