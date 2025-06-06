using UnityEngine;

public class BackButton : StartBackButton
{
    protected override void OnClickButton()
    {
        CreativeDestructionManager.Instance.BackButtonInput();
    }
}
