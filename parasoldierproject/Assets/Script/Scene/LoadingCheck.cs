using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public enum FadeType
{
    FadeIn = 0,
    FadeOut = 1,
}

public class LoadingCheck : MonoBehaviour
{
    [SerializeField]
    CanvasGroup canvasGroup;

    float fadeinTime = 1;
    float fadeoutTime = 1;

    [SerializeField]
    Slider loadingBar;     // UIのスライダー

    [SerializeField]
    TextMeshProUGUI loadingText;      // パーセンテージ表示

    SceneLoader loader;
    private void Awake()
    {
        StartCoroutine(LoadPercentCheck());
    }

    IEnumerator LoadPercentCheck()
    {
        // 変数reset
        Display(0);

        loader = SceneLoader.Instance;

        // フェードイン処理 -----------------------------------------------------------------------
        yield return Fade(fadeinTime,FadeType.FadeIn);

        // フェードイン完了通達 -------------------------------------------------------------------
        loader.LoadUIAllSet();

        // ローディング%画面処理 ------------------------------------------------------------------
        while (!loader.CheckLoadended || !Display(loader.Percent)) { yield return new WaitForSeconds(1 / 60); }

        // フェードアウト処理 --------------------------------------------------------------------
        yield return Fade(fadeoutTime, FadeType.FadeOut);

        // フェードアウト終了通達 ----------------------------------------------------------------
        loader.LoadAllClear();
    }

    IEnumerator Fade(float maxtime, FadeType fade)
    {
        // フェードイン処理
        float time = 0;
        float percent = 0;

        while (time < maxtime)
        {
            time += Time.deltaTime;
            percent = Mathf.Abs((int)fade -  Mathf.Clamp01(time / maxtime));
            canvasGroup.alpha = percent;
            yield return new WaitForSeconds(1 / 60);
        }
    }

    private bool Display(float percent)
    {
        if(loadingBar != null) loadingBar.value = percent;
        if(loadingText != null) loadingText.text = (percent * 100f).ToString("F0") + "%";
        return (percent == 1);
    }
}
