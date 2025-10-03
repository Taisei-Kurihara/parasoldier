using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
/// ステータスビューの抽象基底クラス
/// </summary>
public abstract class StatusView_abstract : MonoBehaviour
{
    [SerializeField] protected Image HPgage;
    [SerializeField] protected Image MPgage;
    [SerializeField] protected Image Extragage;


    [SerializeField] protected int MPstock = 5;
    [SerializeField] protected int Extrastock = 3;


    public int MPmax { get { return MPstock; } }
    public int Extramax { get { return Extrastock; } }


    protected ReactiveProperty<int> nowmp = new ReactiveProperty<int>(0);
    protected ReactiveProperty<int> nowextra = new ReactiveProperty<int>(0);
    public bool ChangeMp(int add)
    {
        if (nowmp.Value + add <= MPstock && nowmp.Value + add >= 0)
        {
            nowmp.Value += add;
        }
        else { return false; }
        return true;
    }

    public bool ChangeExtra(int add)
    {
        if (nowextra.Value + add <= Extrastock && nowextra.Value + add >= 0)
        {
            nowextra.Value += add;
        }
        else { return false; }
        return true;
    }

    /// <summary>
    /// 初期化処理（inputModel によって左右切り替え）
    /// </summary>
    public virtual void Init(CharacterType inputModel)
    {
        // すべて Filled に設定
        if (HPgage != null) HPgage.type = Image.Type.Filled;
        if (MPgage != null) MPgage.type = Image.Type.Filled;
        if (Extragage != null) Extragage.type = Image.Type.Filled;

        // FillMethod を横方向に
        if (HPgage != null) HPgage.fillMethod = Image.FillMethod.Horizontal;
        if (MPgage != null) MPgage.fillMethod = Image.FillMethod.Horizontal;
        if (Extragage != null) Extragage.fillMethod = Image.FillMethod.Horizontal;

        // inputModel によって左右切り替え
        int origin = (inputModel == CharacterType.Player) ? 0 : 1;
        // 0 = Left, 1 = Right

        if (HPgage != null) HPgage.fillOrigin = origin;
        if (MPgage != null) MPgage.fillOrigin = origin;
        if (Extragage != null) Extragage.fillOrigin = origin;

        // 初期状態は満タン
        if (HPgage != null) HPgage.fillAmount = 1f;
        if (MPgage != null) MPgage.fillAmount = 0f;
        if (Extragage != null) Extragage.fillAmount = 0f;


        nowmp.Subscribe(x => { if (MPgage != null) MPgage.fillAmount = (float)x / MPstock; }).Dispose();
        nowextra.Subscribe(x => { if (Extragage != null) Extragage.fillAmount = (float)x / Extrastock; }).Dispose();

        Debug.Log($"{inputModel} のステータスビューを初期化しました (fillOrigin={origin})");
    }

    public float HPset
    {
        set { if (HPgage != null) HPgage.fillAmount = Mathf.Clamp01(value); }
    }

    public float MPset
    {
        set { if (MPgage != null) MPgage.fillAmount = Mathf.Clamp01(value); }
    }

    public float Extragageset
    {
        set { if (Extragage != null) Extragage.fillAmount = Mathf.Clamp01(value); }
    }
}
