using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System.Collections;

public class SelectButton : MonoBehaviour
{
    int Num = 0; // ボタンの番号
    public int GetNum { get { return Num; } }
    BitArray isSelected = new BitArray(1, false); // ボタンが選択されているかどうか

    public void Init(int Num)
    {
        this.Num = Num;
        if (Num != 0) { AwaitButton(); }
        else
        {
            GetComponent<Button>().image.color = Color.gray;
        }
    }

    public void AwaitButton()
    {
        GetComponent<Button>().image.color = Color.white; // ボタンの色を白に設定
        GetComponent<Button>().OnClickAsObservable()
            .Take(1)
            .Subscribe(button => { GetComponent<Button>().image.color = Color.gray; CreativeDestructionManager.Instance.NowSelectButton = this; })
            .AddTo(this);
    }
}
