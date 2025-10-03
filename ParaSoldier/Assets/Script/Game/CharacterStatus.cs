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

    public void DamageReaction(float damage, float blowPower, float blowTime)
    {
        hp.Value -= damage;
        characterMove.DamageReaction(blowPower, blowTime);
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
