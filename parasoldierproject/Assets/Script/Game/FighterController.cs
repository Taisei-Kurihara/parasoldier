// FighterController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using static UnityEngine.Rendering.DebugUI;

public class FighterController : MonoBehaviour
{
    [Header("移動・基本設定")]
    public float moveSpeed = 3f; // 最大移動速度
    public float acceleration = 10f; // 加速度
    private float currentSpeed = 0f; // 現在の速度

    [Header("入力・状態制御")]
    public float inputCooldown = 0.01f; // 各行動後の入力リキャスト
    private float lastInputTime; // 最後に入力を受け付けた時間
    private bool isInHitStop = false; // ヒットストップ中かどうか
    private float actionStunTime = 0f; // 行動による硬直時間
    private bool isGuarding = false; // 現在ガード中かどうか

    [Header("コンボ管理")]
    private int normalAttackStage = 0; // 現在の通常攻撃段階（1～3）
    private float lastAttackTime; // 最後の攻撃入力時間
    private float comboWindow1 = 0.2f; // 1→2段目入力受付時間
    private float comboWindow2 = 0.15f; // 2→3段目入力受付時間

    [Header("クールタイム")]
    private float waterCooldown = 3f; // 水球攻撃のクールタイム
    private float chargeCooldown = 3f; // 突進攻撃のクールタイム
    private float lastWaterTime = -999f; // 最後の水球攻撃時間
    private float lastChargeTime = -999f; // 最後の突進攻撃時間

    [Header("ゲージ・HP")]
    public float gauge = 7; // 現在のゲージ数
    public float maxGauge = 7; // ゲージ最大値
    public float hp = 10; // 現在のHP
    public float maxHP = 10; // HP最大値
    private float gaugeRecoveryRate = 1f / 2.5f; // 2.5秒で1ゲージ回復
    private float gaugeRecoveryProgress = 0f; // ゲージ回復進捗

    [Header("UI表示")]
    public Image hpText; // HP表示UI
    public Image gaugeText; // ゲージ表示UI

    [Header("攻撃判定")]
    public GameObject hitBoxPrefab; // 攻撃用ヒットボックスプレハブ
    public Transform hitBoxSpawnPoint; // ヒットボックス生成位置

    void Update()
    {
        // 行動不能条件のチェック（ヒットストップや硬直など）
        if (isInHitStop || Time.time - lastInputTime < inputCooldown || actionStunTime > 0f)
        {
            actionStunTime -= Time.deltaTime;
            UpdateUI(); // UIは常時更新
            return;
        }

        lastInputTime = Time.time;

        HandleMovement(); // 移動入力
        HandleActions();  // 攻撃・ガード・回復
        UpdateUI();       // HP・ゲージUI更新
    }

    void HandleMovement()
    {
        // ガードまたは硬直中は移動不可
        if (isGuarding || actionStunTime > 0f) return;

        float moveInput = 0f;
        if (Input.GetKey(KeyCode.RightArrow)) moveInput = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow)) moveInput = -1f;

        float targetSpeed = moveInput * moveSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        transform.position += new Vector3(currentSpeed, 0, 0) * Time.deltaTime;

        // 向きを入力方向に調整
        if (moveInput != 0) transform.localScale = new Vector3(Mathf.Sign(moveInput), 1, 1);
    }

    void HandleActions()
    {
        // Vキーでガード（押し続けている間）
        isGuarding = Input.GetKey(KeyCode.V);

        if (Input.GetKeyDown(KeyCode.Z)) HandleNormalAttack(); // 通常攻撃
        if (Input.GetKeyDown(KeyCode.X)) HandleWaterAttack();  // 水球攻撃
        if (Input.GetKeyDown(KeyCode.C)) HandleChargeAttack(); // 突進攻撃

        if (Input.GetKey(KeyCode.B)) RecoverGauge(); // ゲージ回復
        else gaugeRecoveryProgress = 0f;
    }

    void HandleNormalAttack()
    {
        float t = Time.time - lastAttackTime;
        if (normalAttackStage == 0 && t >= 0.5f) DoNormalAttack(1);
        else if (normalAttackStage == 1 && t <= comboWindow1) DoNormalAttack(2);
        else if (normalAttackStage == 2 && t <= comboWindow2) DoNormalAttack(3);
        else
        {
            normalAttackStage = 0;
            Debug.Log("コンボ失敗");
        }
    }

    void DoNormalAttack(int stage)
    {
        lastAttackTime = Time.time;
        normalAttackStage = stage;
        StartCoroutine(HitStop(0.1f)); // ヒットストップ効果
        actionStunTime = 0.3f + stage * 0.1f; // 段階に応じた硬直
        SpawnHitBox(stage); // 攻撃段階ごとにタイプ指定

        // ステージ1～3終了で1ゲージ削除（3ヒットで1ゲージ）
        if (stage == 3 && gauge > 0)
        {
            gauge = Mathf.Max(gauge - 1, 0);
        }
    }

    void HandleWaterAttack()
    {
        if (Time.time - lastWaterTime >= waterCooldown && gauge > 0)
        {
            lastWaterTime = Time.time;
            gauge--;
            StartCoroutine(HitStop(0.1f));
            actionStunTime = 1.0f;
            SpawnHitBox(4); // 水球攻撃（ダメージ4）
        }
    }

    void HandleChargeAttack()
    {
        if (Time.time - lastChargeTime >= chargeCooldown)
        {
            lastChargeTime = Time.time;
            StartCoroutine(HitStop(0.1f));
            actionStunTime = 0.6f;
            SpawnHitBox(5); // 突進攻撃（ダメージ5）
        }
    }

    void RecoverGauge()
    {
        gaugeRecoveryProgress += Time.deltaTime * gaugeRecoveryRate;
        if (gaugeRecoveryProgress >= 1f)
        {
            int recovered = Mathf.FloorToInt(gaugeRecoveryProgress);
            gauge = Mathf.Min(gauge + recovered, maxGauge);
            gaugeRecoveryProgress -= recovered;
        }
    }

    IEnumerator HitStop(float duration)
    {
        isInHitStop = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        isInHitStop = false;
    }

    void SpawnHitBox(int attackType)
    {
        GameObject obj = Instantiate(hitBoxPrefab, hitBoxSpawnPoint.position, Quaternion.identity);
        //HitBox hit = obj.GetComponent<HitBox>();
        //hit.owner = this;
        //hit.attackType = attackType;
        // バグってるので一時的にコメントアウト
    }

    public void TakeDamage(int dmg)
    {
        hp = Mathf.Max(hp - dmg, 0);
        if (hp <= 0) Debug.Log("敗北");
    }

    void UpdateUI()
    {
        if (hpText != null) hpText.fillAmount = hp/maxHP;
        if (gaugeText != null) gaugeText.fillAmount = gauge/maxGauge;
    }
}
