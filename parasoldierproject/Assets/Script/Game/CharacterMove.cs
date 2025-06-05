using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary> キャラクタの動きだけを管理する </summary>
public class CharacterMove : MonoBehaviour
{
    CancellationToken token;
    Rigidbody rigidbody;

    #region 入力管理
    /// <summary> 入力管理 </summary>
    BitArray acceptInput = new BitArray(2, false);

    /// <summary> 現在移動中かどうか </summary>
    public bool NowMove { get { return acceptInput[0]; } set { acceptInput[0] = value; } }

    /// <summary> 移動可能な状態か確認する </summary>
    public bool CanItBeMoved { get { return acceptInput[1]; } }
    public void UnMove() { acceptInput[1] = false; }
    public void OnMove() { acceptInput[1] = true; }

    #endregion

    private void Awake()
    {
        token = this.GetCancellationTokenOnDestroy();
        rigidbody = GetComponent<Rigidbody>();
    }

    #region 移動処理

    [SerializeField]
    MoveData moveData;

    public void OnMoveInput(float moveDis)
    {
        moveData.MoveDis = moveDis;

        if (!NowMove)
        {
            MoveAsync().Forget();
        }
    }

    public void OutMoveInput()
    {
        moveData.MoveDis = 0;
    }

    async UniTask MoveAsync()
    {
        while (moveData.MoveDis != 0)
        {
            await UniTask.WaitUntil(() => CanItBeMoved || moveData.MoveDis == 0 || token.IsCancellationRequested);
            rigidbody.linearVelocity = ((Vector3.right * moveData.MoveDis) * moveData.Speed) * Time.deltaTime;
        }

        NowMove = false; // 移動が完了したら移動中フラグを下ろす
    }

    #endregion
}


[System.Serializable]
public class MoveData
{
    [SerializeField]
    float speed = 5f;
    public float Speed { get { return speed; } }
    
    float moveDis = 0f;
    public float MoveDis { get { return moveDis; } set { moveDis = Mathf.Clamp01(value); } }





    public MoveData(float speed)
    {
        this.speed = speed;
    }
}
