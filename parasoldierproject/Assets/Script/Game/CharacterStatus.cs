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

    public void DamageReaction(float damage, float blowPower, float blowTime)
    {
        hp.Value -= damage;
        characterMove.DamageReaction(blowPower, blowTime);
    }


}
