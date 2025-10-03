using UnityEngine;

public abstract class InputModel_EnemyAI<T> : MonoBehaviour, InputModel_interface where T : Enemy_AI_Level_interface
{
    // �L�����N�^�[�v���[���^�[�ւ̎Q��
    // AI���䂩��L�����N�^�[�̓���𐧌䂷�邽�߂Ɏg�p
    public CharacterPresenter CP { get; set; }

    protected abstract T AI_Level { get; set; }
    
    // 入力設定の初期化 - 継承先で処理は空の状態で記述
    public virtual void SetUpInput()
    {
        // 継承先で実装
    }
}
