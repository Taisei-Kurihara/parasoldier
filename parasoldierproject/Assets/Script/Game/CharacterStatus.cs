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
    

    const float OneGage = 100f / 5f;

    [HideInInspector]
    public ReactiveProperty<float> gage = new ReactiveProperty<float>(0);

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
        gage.Value = Mathf.Clamp(gage.Value + (amount * OneGage), 0, 100f); // 例：最大値100として制限
    }

    public int CheckGage()
    {
        return (int)(gage.Value / OneGage); // 例：5段階に分ける
    }
}

