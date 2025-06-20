using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

public enum CharacterState
{
    Idle,
    Move,
    Attack,
    WaterShot,
    Charge,
    Assault,
    Guard,
    DamageReaction
}

/// <summary> キャラクタの動きだけを管理する </summary>
public class CharacterMove : MonoBehaviour
{
    [SerializeField]
    Animator animator;

    CancellationToken token;
    Rigidbody rigidbody;

    #region 入力管理

    #region 全体
    BitArray generalOrder = new BitArray(2, true);

    /// <summary> 戦闘前or決着後に動けなくする用 </summary>
    public bool inputFailure { get { return generalOrder[0]; } }
    private void UnlockInput() { generalOrder[0] = false; }
    private void DisableInput() { generalOrder[0] = true; }

    // / <summary> 入力割り込みできるか </summary>
    public bool Interrupt { get { return generalOrder[1]; } set { generalOrder[1] = value; } }

    #endregion

    #region 移動入力管理

    BitArray moveInput = new BitArray(2, false);

    /// <summary> 現在移動入力中かどうか </summary>
    public bool NowMoveInput { get { return moveInput[0]; } }
    private bool SetNowMoveInput { set { moveInput[0] = value; } }


    /// <summary> 移動可能な状態か確認する </summary>
    public bool CanItBeMoved { get { return moveInput[1]; } }
    private void UnMove() { moveInput[1] = false; }
    private void OnMove() { moveInput[1] = true; }
    #endregion

    #region 攻撃入力管理
    BitArray attackInput = new BitArray(2, false);

    public bool attack_01 { get { return attackInput[0]; } set { attackInput[0] = value; } }

    #endregion

    #endregion


    private void Awake()
    {
        token = this.GetCancellationTokenOnDestroy();
        rigidbody = GetComponent<Rigidbody>();

        if (rigidbody == null) { Debug.LogError(gameObject.name + ":none rb"); }

        Init();
    }


    public void Init()
    {
        UnlockInput();
        moveData.moveDis
            .Subscribe(_ => { if (!NowMoveInput) { MoveAsync().Forget(); } } )
            .AddTo(this);

    }



    #region 移動処理

    [SerializeField]
    public MoveData moveData = new MoveData(100);

    async UniTask MoveAsync()
    {
        SetNowMoveInput = true;

        while (moveData.moveDis.Value != 0)
        {
            if (CanItBeMoved || Interrupt)
            {
                rigidbody.linearVelocity = ((Vector3.right * moveData.moveDis.Value) * moveData.Speed) * Time.deltaTime;
            }

            animator.SetBool("isWalk", (CanItBeMoved || Interrupt));


            await UniTask.Yield(token);
        }

        animator.SetBool("isWalk", false);

        SetNowMoveInput = false; // 移動が完了したら移動中フラグを下ろす
    }
    #endregion

    #region 攻撃処理

    public void Attack()
    {
        if (inputFailure || !Interrupt) return; // 入力が無効化されている場合は攻撃しない
        Attackasync().Forget();
    }

    async UniTask Attackasync()
    {
        Interrupt = false; // 攻撃中は入力割り込みを無効化
        // 攻撃処理
        animator.SetTrigger("attack");
        await UniTask.Yield(token);
        await UniTask.Delay(1000, cancellationToken: token); // 攻撃アニメーションの再生時間に合わせて待機
        Interrupt = true; // 攻撃が終わったら入力割り込みを有効化
    }

    #endregion

    public async UniTask DamageReaction()
    {
        Interrupt = false;
        await UniTask.Yield(token);
        Interrupt = true;
    }

    private void Update()
    {
        AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfos.Length > 0)
        {
            string clipName = clipInfos[0].clip.name;
            Debug.Log($"Now playing clip : {clipName}");
        }
    }
}


[System.Serializable]
public class MoveData
{
    [SerializeField]
    float speed = 5f;
    public float Speed { get { return speed; } }

    public ReactiveProperty<float> moveDis { get; private set; } = new(0);

    public MoveData(float speed)
    {
        this.speed = speed;
    }
}
