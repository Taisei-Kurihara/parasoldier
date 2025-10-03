using UnityEngine;

public class texttest : MonoBehaviour
{
    public static texttest instance { get; private set; }

    public static texttest Instance
    {
        get
        {
            if (instance == null)
            {
                // 既存のインスタンスを探す
                instance = FindObjectOfType<texttest>();

                // なければ新規生成
                if (instance == null)
                {
                    GameObject obj = new GameObject("texttest");
                    instance = obj.AddComponent<texttest>();
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

    public string text = "";

    public void addst(string st) { text += st + "\n"; }

}
