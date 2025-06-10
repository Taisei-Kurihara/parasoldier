using UnityEngine;
using UniRx;

/// <summary> ゲームの進行 </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 既存のインスタンスを探す
                instance = FindObjectOfType<GameManager>();

                // なければ新規生成
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
        // シングルトン化
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject); // シーンをまたいでも保持
    }

    HPManager hpManager;
    public HPManager HpManager { get { return hpManager; } set { hpManager = value; HpZeroCheck(); } }

    private void HpZeroCheck()
    {
        hpManager.orHPzero
            .Take(1) // 1回だけ購読
            .Subscribe(isZero =>
            {
                if (isZero == 1)
                {
                    Debug.Log("Player 1's HP is zero or below.");
                }
                else if (isZero == 0)
                {
                    Debug.Log("Player 2's HP is zero or below.");
                }
            })
            .AddTo(this);
    }

}
