using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Unity.VisualScripting;

public abstract class StartBackButton : MonoBehaviour
{
    protected void Awake()
    {
        OnClick().Forget();
    }

    protected async UniTask OnClick()
    {
        await UniTask.WaitUntil(() => CreativeDestructionManager.Instance.StartButtonInputCheck);

        this.GetComponent<Button>().OnClickAsObservable()
            .Take(1)
            .Subscribe(_ => { OnClickButton(); })
            .AddTo(this);
    }

    abstract protected void OnClickButton();
}
