using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

/// <summary> キャラクタの動きだけを管理する </summary>
public class CharacterMove : MonoBehaviour
{
    [SerializeField]
    Animator animator;

    CancellationToken token;
    Rigidbody rigidbody;

    #region 入力管理
    /// <summary> 入力管理 </summary>
    BitArray acceptInput = new BitArray(2, false);

    #region 移動入力管理
    /// <summary> 現在移動中かどうか </summary>
    public bool NowMove { get { return acceptInput[0]; } }
    private bool SetNowMove { set { acceptInput[0] = value; } }

    /// <summary> 移動可能な状態か確認する </summary>
    public bool CanItBeMoved { get { return acceptInput[1]; } }
    private void UnMove() { acceptInput[1] = false; }
    private void OnMove() { acceptInput[1] = true; }
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
        OnMove();

        moveData.moveDis
            .Subscribe(_ => { if (!NowMove) { MoveAsync().Forget(); } } )
            .AddTo(this);
    }

    #region 移動処理

    [SerializeField]
    public MoveData moveData = new MoveData(100);

    async UniTask MoveAsync()
    {
        SetNowMove = true;

        while (moveData.moveDis.Value != 0)
        {
            await UniTask.WaitUntil(() => (CanItBeMoved || moveData.moveDis.Value == 0) || token.IsCancellationRequested);
            rigidbody.linearVelocity = ((Vector3.right * moveData.moveDis.Value) * moveData.Speed) * Time.deltaTime;
        }

        SetNowMove = false; // 移動が完了したら移動中フラグを下ろす
    }

    #endregion

    public void DamageReaction()
    {

    }
    void Update()
    {
        int layer = 0;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
        AnimatorStateInfo nextStateInfo = animator.GetNextAnimatorStateInfo(layer);
        AnimatorTransitionInfo transInfo = animator.GetAnimatorTransitionInfo(layer);

        if (animator.IsInTransition(layer))
        {
            // トランジション中。複数クリップが混ざっている可能性がある
            var currentClips = animator.GetCurrentAnimatorClipInfo(layer);
            var nextClips = animator.GetNextAnimatorClipInfo(layer);
            // currentClips[0].clip と nextClips[0].clip の両方が存在する可能性
            Debug.Log($"Transition: from {stateInfo.shortNameHash} to {nextStateInfo.shortNameHash}");
            foreach (var ci in currentClips)
                Debug.Log("  current clip: " + ci.clip.name);
            foreach (var ni in nextClips)
                Debug.Log("  next clip: " + ni.clip.name);
        }
        else
        {
            // 通常再生中
            var clipInfos = animator.GetCurrentAnimatorClipInfo(layer);
            if (clipInfos.Length > 0)
                Debug.Log("現在のクリップ: " + clipInfos[0].clip.name);
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
