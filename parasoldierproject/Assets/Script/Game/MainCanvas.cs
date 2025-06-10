using UnityEngine;
using UniRx;
public class MainCanvas : MonoBehaviour
{
    private void Awake()
    {
        Observable
            .EveryUpdate()
            .Where(_ => CreativeDestructionManager.Instance.MainCanvasData == null)
            .Take(1)
            .Subscribe(_ => { CreativeDestructionManager.Instance.MainCanvasData = this; })
            .AddTo(this);
        
    }
}
