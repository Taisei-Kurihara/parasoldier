using Common;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CharacterType
{
    Player,
    AI
}

public abstract class CharacterPresenter : MonoBehaviour
{
    StatusView_abstract View;

    protected virtual SkillModel_abstract DefaultSkillModel => null; // デフォルトのスキルモデルを設定する場合は継承先でオーバーライドする
    SkillModel_abstract SkillModel;

    protected virtual InputModel_interface DefaultEnemyInputModel => null; // デフォルトのAI入力モデルを設定する場合は継承先でオーバーライドする
    InputModel_interface InputModel;

    [SerializeField]
    Animator character;
    [SerializeField]
    Animator weapon;
    [SerializeField]
    CapsuleCollider BodyColl;

    public void DefaulInputlInitialize(StatusView_abstract view, CharacterType inputModel)
    {
        InputModel_interface defo = null;
        switch (inputModel)
        {
            case CharacterType.AI:
                defo = gameObject.AddComponent<InputModel_EnemyAI_Test>();
                defo.CP = this;
                break;
            default:
                defo = gameObject.AddComponent<InputModel_Player>();
                defo.CP = this;
                break;
        }


        Initialize(view, DefaultSkillModel, defo);
    }
    public void Initialize(StatusView_abstract view, SkillModel_abstract skillModel, InputModel_interface inputModel)
    {
        View = view;
        SkillModel = skillModel;
        InputModel = inputModel;

        InputModel?.SetUpInput();
    }

    #region 入力通知
    public void Move(float x)
    {
        if (SkillModel == null) { Debug.Log("null move"); }
        SkillModel?.OnMove(x);
    }

    public void OnAttack()
    {
        SkillModel?.OnAttack();
    }

    public void OnWaterShot()
    {
        SkillModel?.OnWaterShot();
    }

    public void OnAssault()
    {
        SkillModel?.OnAssault();
    }

    public void OnCharge()
    {
        SkillModel?.OnCharge();
    }

    public void OutCharge()
    {
        SkillModel?.OutCharge();
    }

    public void OnGuard()
    {
        SkillModel?.OnGuard();
    }

    public void OutGuard()
    {
        SkillModel?.OutGuard();
    }
    #endregion
}