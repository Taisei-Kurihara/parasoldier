using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using R3.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

#region enum
public enum CharacterState
{
    Idle,              // �ҋ@
    Move,              // �ړ�
    Attack_01,         // �U��1
    Attack_02,         // �U��2
    Attack_03,         // �U��3
    WaterShot,         // ����
    Charge,            // �`���[�W
    Assault,           // �ːi
    Guard,             // �h�䒆
    DamageReaction,    // �_���[�W���A�N�V����
    GuardTransition    // �K�[�h�ڍs��
}

// �U���̋������x��
public enum PowerLevel { Lv0 = 0, Lv1, Lv2, Lv3, Lv4 }
// ������ёϐ����x��
public enum BlowResistLevel { Lv0 = 0, Lv1, Lv2, Lv3, Lv4 }
// �_���[�W�y�������x��
public enum DamageReduceLevel { Lv0, Lv1, Lv2, Lv3, Lv4 }
// �h����
public enum GuardState { None, Transition, Guarding }
#endregion


public class DynamicObjectController_Game : DynamicObjectController_interface
{

    string playercharacter = "Test";
    string enemycharacter = "Test";

    Canvas mainCanvas;

    public async UniTask OnSceneLoadedAsync()
    {
        // Player UI
        var playerUIPrefab = await DynamicObjectManager.Instance().LoadAssetAsync<GameObject>("Status_UI_" + playercharacter);
        var playerUIObj = GameObject.Instantiate(playerUIPrefab);
        PlayerView = playerUIObj.GetComponent<StatusView_abstract>();
        if (PlayerView != null) PlayerView.Init(CharacterType.Player);
        
        // Attach Player UI to UImanager's canvas
        await UImanager.Instance().AttachToCanvas(playerUIObj);

        // Player Character
        var playerPrefab = await DynamicObjectManager.Instance().LoadAssetAsync<GameObject>("Character_" + playercharacter);
        var playerObj = GameObject.Instantiate(playerPrefab);
        PlayerTransform = playerObj.transform;
        PlayerTransform.name = "Player_" + playercharacter;
        PlayerTransform.position = new Vector3(-5, 1, 0);
        var playerCP = PlayerTransform.GetComponent<CharacterPresenter>();
        playerCP.DefaulInputlInitialize(PlayerView, CharacterType.Player);

        // Enemy UI
        var enemyUIPrefab = await DynamicObjectManager.Instance().LoadAssetAsync<GameObject>("Status_UI_" + enemycharacter);
        var enemyUIObj = GameObject.Instantiate(enemyUIPrefab);
        EnemyView = enemyUIObj.GetComponent<StatusView_abstract>();
        if (EnemyView != null) EnemyView.Init(CharacterType.AI);
        
        // Attach Enemy UI to UImanager's canvas
        await UImanager.Instance().AttachToCanvas(enemyUIObj);

        // Enemy Character
        var enemyPrefab = await DynamicObjectManager.Instance().LoadAssetAsync<GameObject>("Character_" + enemycharacter);
        var enemyObj = GameObject.Instantiate(enemyPrefab);
        EnemyTransform = enemyObj.transform;
        EnemyTransform.name = "Enemy_" + enemycharacter;
        EnemyTransform.position = new Vector3(5, 1, 0);
        EnemyTransform.localScale = new Vector3(EnemyTransform.localScale.x, EnemyTransform.localScale.y, -EnemyTransform.localScale.z);
        var enemyCP = EnemyTransform.GetComponent<CharacterPresenter>();
        enemyCP.DefaulInputlInitialize(EnemyView, CharacterType.AI);

        //// Stage
        //var stagePrefab = await DynamicObjectManager.Instance().LoadAssetAsync<GameObject>("stage");
        //var stageObj = GameObject.Instantiate(stagePrefab);
        //stageObj.transform.position = Vector3.down;
    }


    public async UniTask OnSceneUnloadedAsync()
    {
    }


    public ReactiveProperty<CharacterState> p1State = new(CharacterState.Idle);
    public ReactiveProperty<CharacterState> p2State = new(CharacterState.Idle);

    public Transform PlayerTransform { get; set; }
    public Transform EnemyTransform { get; set; }

    StatusView_abstract PlayerView;
    StatusView_abstract EnemyView;

    public const float maxHp = 100f;   // �ő�HP
    public const float maxgage = 100f; // �ő�gage

    public ReactiveProperty<float> p1Hp { get; private set; } = new(maxHp);
    public ReactiveProperty<float> p2Hp { get; private set; } = new(maxHp);
    public ReactiveProperty<float> p1Gage { get; private set; } = new(0f);
    public ReactiveProperty<float> p2Gage { get; private set; } = new(0f);
    public ReactiveProperty<int> orHPzero { get; private set; } = new(0);

    public void ResetHP()
    {
        p1Hp.Value = maxHp;
        p2Hp.Value = maxHp;

        p1Gage.Value = 0f;
        p2Gage.Value = 0f;
        orHPzero.Value = 0;

        HpZeroCheck();
    }
    public void HpZeroCheck()
    {
        orHPzero
            .Where(isZero => isZero == 1 || isZero == 2) // �� �����𖞂������̂����ʂ�
            .Take(1) // �� �ŏ���1��ōw�ǉ���
            .Subscribe(isZero =>
            {
                // ���L�����̓��͂��~

                // ���U���g���o�J�n
                //ShowResultFlow("ResultImage").Forget();
            });
    }
}
