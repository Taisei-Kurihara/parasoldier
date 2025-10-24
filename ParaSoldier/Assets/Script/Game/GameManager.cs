using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Threading;

/// <summary> �Q�[���̐i�s </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                // �����̃C���X�^���X��T��
                instance = FindObjectOfType<GameManager>();

                // �Ȃ���ΐV�K����
                if (instance == null)
                {
                    GameObject obj = new GameObject("GameManager");
                    instance = obj.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        // �V���O���g����
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); // �V�[�����܂����ł��ێ�
    }

    HPManager hpManager;
    public HPManager HpManager { get { return hpManager; } set { hpManager = value; } }
    public int orHPzero => hpManager.orHPzero.Value;

    public ReactiveProperty<CharacterState> p1State = new(CharacterState.Idle);
    public ReactiveProperty<CharacterState> p2State = new(CharacterState.Idle);

    public Transform PlayerTransform { get; set; }
    public Transform EnemyTransform { get; set; }

    public void HpZeroCheck()
    {
        hpManager.orHPzero
            .Where(isZero => isZero == 1 || isZero == 2) // �� �����𖞂������̂����ʂ�
            .Take(1) // �� �ŏ���1��ōw�ǉ���
            .Subscribe(isZero =>
            {
                // ���L�����̓��͂��~
                PlayerTransform.GetComponent<CharacterMove>()?.DisableInput();
                EnemyTransform.GetComponent<CharacterMove>()?.DisableInput();

                //(既)修:最後の攻撃モーションが始まってから0.5 ~ 2 秒ほど 両キャラの移動や攻撃を止めたままディレイ
                //上:両コライダーをistriggerにするのをCharacterStatusではなく別処で行うようにしてほしい
                // ���U���g���o�J�n
                ShowResultFlow("ResultImage").Forget();
            })
            .AddTo(this);

    }

    GameObject nowresult = null;

    GameObject Nowresul {
        get { return nowresult; }
        set {
            if (Nowresul != null) { Destroy(nowresult); }

            nowresult = value;

            Nowresul.SetActive(false); // ���������I�u�W�F�N�g��L����

            Nowresul.transform.parent = CreativeDestructionManager.Instance.MainCanvasData.transform;
            Nowresul.transform.localPosition = Vector3.zero; // Canvas�̒��S�ɔz�u
        }
    }

    private async UniTask ShowResultFlow(string result)
    {
        var token = this.GetCancellationTokenOnDestroy();

        // (既)修:倒されていない方のコライダーだけをisTriggerに設定してください
        // 倒されていない方のキャラクターのコライダーをisTriggerに設定.
        SetCharacterCollidersToTrigger();

        // 最後の攻撃モーションの余韻のためのディレイ (0.5 ~ 2秒).
        await UniTask.Delay(System.TimeSpan.FromSeconds(1.5f), cancellationToken: token);

        Nowresul = await GameSetUp_FlowManager.Instance.AddreLoadAndInstantiateAsync(result, Vector3.zero ,token).AttachExternalCancellation(token);

        GameObject winorlose = await GameSetUp_FlowManager.Instance.AddreLoadAndInstantiateAsync((hpManager.orHPzero.Value == 2)?"win":"lose",Vector3.zero, token).AttachExternalCancellation(token);

        winorlose.transform.parent = Nowresul.transform;
        winorlose.transform.localPosition = Vector3.zero; // ���U���g��ʂ̒��S�ɔz�u

        Nowresul.SetActive(true); // ���U���g��ʂ�\��
    }

    /// <summary> 倒されていない方のキャラクターのコライダーをisTriggerに設定する. </summary>
    private void SetCharacterCollidersToTrigger()
    {
        int winner = hpManager.orHPzero.Value;

        // orHPzero.Value == 1: プレイヤーが倒された（エネミーの勝ち）
        // orHPzero.Value == 2: エネミーが倒された（プレイヤーの勝ち）

        if (winner == 2 && PlayerTransform != null)
        {
            // (既)修:全てのコライダーをisTriggerに設定するようにしてください.
            // プレイヤーが勝った場合、プレイヤーの全てのコライダーをisTriggerに設定.
            var colliders = PlayerTransform.GetComponents<Collider>();
            foreach(var c in colliders)
            {
                c.isTrigger = true;
            }
        }
        else if (winner == 1 && EnemyTransform != null)
        {
            // エネミーが勝った場合、エネミーの全てのコライダーをisTriggerに設定.
            var colliders = EnemyTransform.GetComponents<Collider>();
            foreach(var c in colliders)
            {
                c.isTrigger = true;
            }
        }
    }

    public void OnRetryButton()
    {
        // �Đ퉉�o��ɃZ�b�g�A�b�v
        RetrySetUp().Forget();
    }

    private async UniTask RetrySetUp()
    {
        var token = this.GetCancellationTokenOnDestroy();

        Destroy(Nowresul); // �����̃��U���g��ʂ�j��

        // �퓬�p���ł��邩�`�F�b�N
        if (GameSetUp_FlowManager.Instance.IsLastEnemy)
        {
            // �ŏI���ʉ�ʕ\��
            Nowresul = await GameSetUp_FlowManager.Instance.AddreLoadAndInstantiateAsync("FinalResult", Vector3.zero, this.GetCancellationTokenOnDestroy());
            Nowresul.SetActive(true); // �ŏI���ʉ�ʂ�\��
        }
        else
        {
            GameSetUp_FlowManager.Instance.retry();
        }
        // SetUp�Ăяo��
    }





}
