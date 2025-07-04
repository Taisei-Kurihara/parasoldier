using UnityEngine;
using UniRx;
public class MainCanvas : MonoBehaviour
{
    private void Awake()
    {
        Observable
            .EveryUpdate()
            .Take(1)
            .Subscribe(_ => { CreativeDestructionManager.Instance.MainCanvasData = this; })
            .AddTo(this);
        
    }
}
