using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

// テスト用の敵AI入力モデルクラス
// 敵キャラクターのAI制御をテストするための実装
public class InputModel_EnemyAI_Test : InputModel_EnemyAI<Enemy_AI_Level_Test_interface>
{
    // キャラクタープレゼンターへの参照
    // AI制御からキャラクターの動作を制御するために使用
    public CharacterPresenter CP { get; set; }
    protected override Enemy_AI_Level_Test_interface AI_Level { get; set; }
    
    // 入力設定の初期化 - 空の処理
    public override void SetUpInput()
    {
        // 空の実装
    }
}
