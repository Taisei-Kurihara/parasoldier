using UnityEngine;
using R3;
using System;

namespace Common
{
    /// <summary>
    /// アニメーションからColliderの有効/無効を制御するクラス
    /// AnimationBoolReactiveを継承してCollider制御機能を追加
    /// 攻撃が当たったかどうかも制御に含める
    /// </summary>
    public class ColliderReactive : AnimationBoolReactive
    {
        [SerializeField] private Collider targetCollider;
        [SerializeField] private Collider2D targetCollider2D;
        
        [Header("Hit Detection")]
        [SerializeField] private bool canHit = true; // 攻撃が当たることができるかどうか
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;
        
        // 攻撃が当たったかどうかを管理するReactiveProperty
        public readonly ReactiveProperty<bool> HasHit = new ReactiveProperty<bool>(false);
        
        // HasHit購読解除用
        private IDisposable hasHitSubscription;
        
        /// <summary>
        /// 値変更時のCollider制御
        /// </summary>
        /// <param name="active">Colliderの有効/無効状態</param>
        protected override void OnValueChanged(bool active)
        {
            UpdateColliderState();
        }
        
        /// <summary>
        /// Colliderの状態を更新する
        /// アニメーションのboolとcanHitの両方を考慮する
        /// </summary>
        private void UpdateColliderState()
        {
            // canHitがfalseの場合は、アニメーションboolを無視してColliderをoff
            bool shouldEnable = IsActive.Value && canHit && !HasHit.Value;
            
            // 3D Colliderの制御
            if (targetCollider != null)
            {
                targetCollider.enabled = shouldEnable;
                if (showDebugLog)
                {
                    Debug.Log($"[ColliderReactive] 3D Collider {gameObject.name} set to {shouldEnable} (Animation: {IsActive.Value}, CanHit: {canHit}, HasHit: {HasHit.Value})");
                }
            }
            
            // 2D Colliderの制御
            if (targetCollider2D != null)
            {
                targetCollider2D.enabled = shouldEnable;
                if (showDebugLog)
                {
                    Debug.Log($"[ColliderReactive] 2D Collider {gameObject.name} set to {shouldEnable} (Animation: {IsActive.Value}, CanHit: {canHit}, HasHit: {HasHit.Value})");
                }
            }
        }
        
        /// <summary>
        /// Colliderコンポーネントを設定する
        /// </summary>
        /// <param name="collider">設定する3D Collider</param>
        public void SetCollider(Collider collider)
        {
            targetCollider = collider;
        }
        
        /// <summary>
        /// 2D Colliderコンポーネントを設定する
        /// </summary>
        /// <param name="collider2D">設定する2D Collider</param>
        public void SetCollider2D(Collider2D collider2D)
        {
            targetCollider2D = collider2D;
        }
        
        /// <summary>
        /// 初期化時に自動でColliderを取得する
        /// </summary>
        private void Start()
        {
            // Colliderが設定されていない場合は自動で取得
            if (targetCollider == null && targetCollider2D == null)
            {
                targetCollider = GetComponent<Collider>();
                targetCollider2D = GetComponent<Collider2D>();
                
                if (targetCollider == null && targetCollider2D == null)
                {
                    Debug.LogWarning($"[ColliderReactive] No Collider found on {gameObject.name}");
                }
            }
            
            // HasHitの変更を購読
            hasHitSubscription = HasHit.Subscribe(_ => UpdateColliderState());
        }
        
        #region 攻撃ヒット関連メソッド
        
        /// <summary>
        /// 攻撃がヒットしたことを記録する
        /// </summary>
        public void OnHit()
        {
            HasHit.Value = true;
            if (showDebugLog)
            {
                Debug.Log($"[ColliderReactive] {gameObject.name} Hit detected, disabling collider");
            }
        }
        
        /// <summary>
        /// ヒット状態をリセットする（新しい攻撃のため）
        /// </summary>
        public void ResetHit()
        {
            HasHit.Value = false;
            if (showDebugLog)
            {
                Debug.Log($"[ColliderReactive] {gameObject.name} Hit reset");
            }
        }
        
        /// <summary>
        /// ヒット可能状態を設定する
        /// </summary>
        /// <param name="canHitValue">ヒット可能かどうか</param>
        public void SetCanHit(bool canHitValue)
        {
            canHit = canHitValue;
            UpdateColliderState();
            if (showDebugLog)
            {
                Debug.Log($"[ColliderReactive] {gameObject.name} CanHit set to {canHit}");
            }
        }
        
        /// <summary>
        /// ヒット可能状態を有効にする（アニメーションイベント用）
        /// </summary>
        public void EnableHit()
        {
            SetCanHit(true);
        }
        
        /// <summary>
        /// ヒット可能状態を無効にする（アニメーションイベント用）
        /// </summary>
        public void DisableHit()
        {
            SetCanHit(false);
        }
        
        #endregion
        
        /// <summary>
        /// クリーンアップ処理
        /// </summary>
        private void OnDestroy()
        {
            hasHitSubscription?.Dispose();
        }
    }
}