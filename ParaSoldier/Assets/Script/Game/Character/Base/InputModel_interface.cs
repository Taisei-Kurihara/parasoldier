using UnityEngine;

// 入力モデルのインターフェース定義
// すべての入力処理クラスが実装する必要がある基本インターフェース
public interface InputModel_interface
{
    // キャラクタープレゼンターへの参照
    // 入力処理からキャラクターの制御を行うために使用
    public CharacterPresenter CP { get; set; }
    
    // 入力設定の初期化
    void SetUpInput();
}
