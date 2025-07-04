using UniRx;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public enum PType
{
    P1, // プレイヤー1
    P2  // プレイヤー2
}

public class HPManager : MonoBehaviour
{
    [SerializeField]
    Image P1Hp;
    [SerializeField]
    Image P1gage;
    [SerializeField]
    Image P2Hp;
    [SerializeField]
    Image P2gage;

    public const float maxHp = 100f;   // 最大HP
    public const float maxgage = 100f; // 最大gage

    public ReactiveProperty<float> p1Hp { get; private set; } = new(maxHp);
    public ReactiveProperty<float> p2Hp { get; private set; } = new(maxHp);
    public ReactiveProperty<float> p1Gage { get; private set; } = new(0f);
    public ReactiveProperty<float> p2Gage { get; private set; } = new(0f);
    public ReactiveProperty<int> orHPzero { get; private set; } = new(0);

    private void Awake()
    {
        GameManager.Instance.HpManager = this;

        p1Hp
            .Subscribe(hp =>
            {
                P1Hp.fillAmount = hp / maxHp;
                if (hp <= 0)
                {
                    orHPzero.Value = 1;
                }
            })
            .AddTo(this);

        p2Hp
            .Subscribe(hp =>
            {
                P2Hp.fillAmount = hp / maxHp;
                if (hp <= 0)
                {
                    orHPzero.Value = 2;
                }
            })
            .AddTo(this);

        p1Gage
            .Subscribe(gage =>
            {
                P1gage.fillAmount = Mathf.Clamp01(gage / maxgage);
            })
            .AddTo(this);

        p2Gage
            .Subscribe(gage =>
            {
                P2gage.fillAmount = Mathf.Clamp01(gage / maxgage);
            })
            .AddTo(this);
    }

    public void ResetHP()
    {
        p1Hp.Value = maxHp;
        p2Hp.Value = maxHp;

        p1Gage.Value = 0f;
        p2Gage.Value = 0f;
        orHPzero.Value = 0;

        GameManager.Instance.HpZeroCheck();
    }
}
