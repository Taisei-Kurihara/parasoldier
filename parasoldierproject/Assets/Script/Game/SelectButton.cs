using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System.Collections;

public class SelectButton : MonoBehaviour
{
    int Num = 0; // ボタンの番号
    BitArray isSelected = new BitArray(1,false); // ボタンが選択されているかどうか

    public void Init(int Num)
    {
        this.Num = Num;
        AwaitButton();
    }

    void AwaitButton()
    {
        GetComponent<Button>().OnClickAsObservable()
            .Subscribe(button => { })
            .AddTo(this);
    }
}
