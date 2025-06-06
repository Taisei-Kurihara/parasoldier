using UnityEngine;

public class MainCanvas : MonoBehaviour
{
    private void Awake()
    {
        CreativeDestructionManager.Instance.MainCanvasData = this;
    }
}
