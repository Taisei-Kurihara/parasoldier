using UnityEngine;

public class StartButton : StartBackButton
{
    protected override void OnClickButton()
    {
        CreativeDestructionManager.Instance.StartButtonInput();
    }
}
