using UniRx;
using UnityEngine;

public class NextBattleButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<UnityEngine.UI.Button>().OnClickAsObservable()
            .Take(1) // 1回だけクリックを受け付ける
            .Subscribe(_ => GameManager.Instance.OnRetryButton())
            .AddTo(this);
    }
}