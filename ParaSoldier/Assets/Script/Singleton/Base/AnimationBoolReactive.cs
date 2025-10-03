using UnityEngine;
using R3;
using System;

namespace Common
{
    /// <summary>
    /// アニメーションから操作可能なbool値のReactiveProperty
    /// アニメーションイベントから呼び出すことでbool値を切り替え可能
    /// </summary>
    public class AnimationBoolReactive : MonoBehaviour
    {
        // 外部（アニメーションイベントなど）から操作する用
        public readonly ReactiveProperty<bool> IsActive = new ReactiveProperty<bool>(false);
        
        // 購読解除用
        private IDisposable subscription;
        
        // 値変更時のコールバック
        private Action<bool> onValueChanged;
        
        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Awake()
        {
            // ReactivePropertyの購読を設定
            SetupSubscription();
        }
        
        /// <summary>
        /// ReactivePropertyの購読設定
        /// </summary>
        private void SetupSubscription()
        {
            subscription = IsActive
                .Subscribe(active =>
                {
                    // 値変更時の処理
                    OnValueChanged(active);
                    
                    // 外部コールバックがあれば実行
                    onValueChanged?.Invoke(active);
                });
        }
        
        /// <summary>
        /// 値変更時の処理（オーバーライド用）
        /// </summary>
        /// <param name="active">新しい値</param>
        protected virtual void OnValueChanged(bool active)
        {
            // 継承先で実装
        }
        
        #region アニメーションイベント用メソッド
        
        /// <summary>
        /// 値をtrueにする（アニメーションイベントから呼ぶ用）
        /// </summary>
        public void EnableValue()
        {
            IsActive.Value = true;
        }
        
        /// <summary>
        /// 値をfalseにする（アニメーションイベントから呼ぶ用）
        /// </summary>
        public void DisableValue()
        {
            IsActive.Value = false;
        }
        
        /// <summary>
        /// 値を反転する（アニメーションイベントから呼ぶ用）
        /// </summary>
        public void ToggleValue()
        {
            IsActive.Value = !IsActive.Value;
        }
        
        /// <summary>
        /// 指定した値に設定する（アニメーションイベントから呼ぶ用）
        /// </summary>
        /// <param name="value">設定する値</param>
        public void SetValue(bool value)
        {
            IsActive.Value = value;
        }
        
        #endregion
        
        #region 外部からの設定用
        
        /// <summary>
        /// 値変更時のコールバックを設定
        /// </summary>
        /// <param name="callback">コールバック関数</param>
        public void SetCallback(Action<bool> callback)
        {
            onValueChanged = callback;
        }
        
        /// <summary>
        /// 値変更時のコールバックを追加
        /// </summary>
        /// <param name="callback">追加するコールバック関数</param>
        public void AddCallback(Action<bool> callback)
        {
            onValueChanged += callback;
        }
        
        /// <summary>
        /// 値変更時のコールバックを削除
        /// </summary>
        /// <param name="callback">削除するコールバック関数</param>
        public void RemoveCallback(Action<bool> callback)
        {
            onValueChanged -= callback;
        }
        
        /// <summary>
        /// 値変更時のコールバックをクリア
        /// </summary>
        public void ClearCallbacks()
        {
            onValueChanged = null;
        }
        
        #endregion
        
        /// <summary>
        /// クリーンアップ処理
        /// </summary>
        private void OnDestroy()
        {
            // 購読を解除
            subscription?.Dispose();
            
            // コールバックをクリア
            ClearCallbacks();
        }
    }
}