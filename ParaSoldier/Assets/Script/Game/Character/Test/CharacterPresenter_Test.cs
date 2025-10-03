using UnityEngine;

// テスト用のキャラクタープレゼンタークラス
// CharacterPresenterを継承してテスト環境での実装を提供
public class CharacterPresenter_Test : CharacterPresenter
{
    private SkillModel_abstract _defaultSkillModel;
    protected override SkillModel_abstract DefaultSkillModel
    {
        get
        {
            if (_defaultSkillModel == null)
            {
                _defaultSkillModel = gameObject.AddComponent<SkillModel_Test>();
            }
            return _defaultSkillModel;
        }
    }

    // デフォルトの敵AI入力モデル
    // デフォルトのAI入力モデルを設定する場合はここで行う 継承先で設定する
    private InputModel_interface _defaultEnemyInputModel;
    protected override InputModel_interface DefaultEnemyInputModel
    {
        get
        {
            if (_defaultEnemyInputModel == null)
            {
                _defaultEnemyInputModel = gameObject.AddComponent<InputModel_EnemyAI_Test>();
            }
            return _defaultEnemyInputModel;
        }
    }
}