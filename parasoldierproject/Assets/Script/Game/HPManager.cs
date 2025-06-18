using UniRx;
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
    Image P1Hp; // HP表示UI
    [SerializeField]
    Image P1gauge; // HP表示UI
    [SerializeField]
    Image P2Hp; // HP表示UI
    [SerializeField]
    Image P2gauge; // HP表示UI

    public const float maxHp = 100f; // 最大HP

    public ReactiveProperty<float> p1Hp { get; private set; } = new(maxHp);
    public ReactiveProperty<float> p2Hp { get; private set; } = new(maxHp);
    public ReactiveProperty<int> orHPzero { get; private set; } = new(0);

    private void Awake()
    {
        GameManager.Instance.HpManager = this;
        p1Hp
            .Subscribe(hp =>
            {
                P1Hp.fillAmount = hp / maxHp; // HPの割合をUIに反映
                if (hp <= 0)
                {
                    orHPzero.Value = 1; // HPが0以下になったらフラグを立てる
                }
            })
            .AddTo(this);

        p2Hp
            .Subscribe(hp =>
            {
                P2Hp.fillAmount = hp / maxHp; // HPの割合をUIに反映
                if (hp <= 0)
                {
                    orHPzero.Value = 2; // HPが0以下になったらフラグを立てる
                }
            })
            .AddTo(this);
    }

    public void ResetHP()
    {
        p1Hp.Value = maxHp; // プレイヤー1のHPをリセット
        p2Hp.Value = maxHp; // プレイヤー2のHPをリセット
        orHPzero.Value = 0; // フラグをリセット
    }


}
