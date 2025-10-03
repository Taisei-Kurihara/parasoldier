using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 通常のシーンローディングクラス
/// フェードイン・フェードアウト効果を使ったシーン遷移を実装する
/// </summary>
public class LoadScene_Normal : MonoBehaviour, LoadScene_interface
{
    // フェード用のキャンバスグループ
    [SerializeField] CanvasGroup fadeCanvas;

    // 経過時間
    float time = 0;
    // フェード変化にかかる時間（秒）
    float changetime = 1;

    /// <summary>
    /// フェードイン処理を開始する
    /// 画面を徐々に明るくする（アルファ値を1から0へ）
    /// </summary>
    public async UniTask StartFadeIn()
    {
        fadeCanvas.alpha = 0f;
        time = 0;
        while (fadeCanvas.alpha < 1f)
        {
            time += Time.deltaTime;
            fadeCanvas.alpha = time / changetime;
            await UniTask.Yield();
        }
    }

    /// <summary>
    /// フェードアウト処理を開始する
    /// 画面を徐々に暗くする（アルファ値を0から1へ）
    /// </summary>
    public async UniTask StartFadeOut()
    {
        // 簡単なフェードイン処理
        fadeCanvas.alpha = 1f;
        time = 0;
        while (fadeCanvas.alpha > 0f)
        {
            time += Time.deltaTime;
            fadeCanvas.alpha = ((time / changetime) - 1) * -1;
            await UniTask.Yield();
        }
    }
}
