using UnityEngine;
using System.Collections.Generic;

namespace Common
{
    /// <summary>
    /// アニメーションから複数のコンポーネントの有効/無効を制御するクラス
    /// </summary>
    public class ComponentReactive : AnimationBoolReactive
    {
        [Header("Target Components")]
        [SerializeField] private List<Behaviour> targetComponents = new List<Behaviour>();
        [SerializeField] private List<Collider> targetColliders = new List<Collider>();
        [SerializeField] private List<Collider2D> targetCollider2Ds = new List<Collider2D>();
        
        [Header("Settings")]
        [SerializeField] private bool inverseControl = false; // trueの時、値を反転して適用
        
        /// <summary>
        /// 値変更時のコンポーネント制御
        /// </summary>
        /// <param name="active">コンポーネントの有効/無効状態</param>
        protected override void OnValueChanged(bool active)
        {
            // 反転制御が有効な場合は値を反転
            bool targetState = inverseControl ? !active : active;
            
            // 汎用Behaviourコンポーネントの制御
            foreach (var component in targetComponents)
            {
                if (component != null)
                {
                    component.enabled = targetState;
                }
            }
            
            // 3D Colliderの制御
            foreach (var collider in targetColliders)
            {
                if (collider != null)
                {
                    collider.enabled = targetState;
                }
            }
            
            // 2D Colliderの制御
            foreach (var collider2D in targetCollider2Ds)
            {
                if (collider2D != null)
                {
                    collider2D.enabled = targetState;
                }
            }
        }
        
        #region コンポーネント管理メソッド
        
        /// <summary>
        /// 制御対象のコンポーネントを追加
        /// </summary>
        public void AddComponent(Behaviour component)
        {
            if (component != null && !targetComponents.Contains(component))
            {
                targetComponents.Add(component);
            }
        }
        
        /// <summary>
        /// 制御対象の3D Colliderを追加
        /// </summary>
        public void AddCollider(Collider collider)
        {
            if (collider != null && !targetColliders.Contains(collider))
            {
                targetColliders.Add(collider);
            }
        }
        
        /// <summary>
        /// 制御対象の2D Colliderを追加
        /// </summary>
        public void AddCollider2D(Collider2D collider2D)
        {
            if (collider2D != null && !targetCollider2Ds.Contains(collider2D))
            {
                targetCollider2Ds.Add(collider2D);
            }
        }
        
        /// <summary>
        /// 制御対象のコンポーネントを削除
        /// </summary>
        public void RemoveComponent(Behaviour component)
        {
            targetComponents.Remove(component);
        }
        
        /// <summary>
        /// 制御対象の3D Colliderを削除
        /// </summary>
        public void RemoveCollider(Collider collider)
        {
            targetColliders.Remove(collider);
        }
        
        /// <summary>
        /// 制御対象の2D Colliderを削除
        /// </summary>
        public void RemoveCollider2D(Collider2D collider2D)
        {
            targetCollider2Ds.Remove(collider2D);
        }
        
        /// <summary>
        /// 全ての制御対象をクリア
        /// </summary>
        public void ClearAllTargets()
        {
            targetComponents.Clear();
            targetColliders.Clear();
            targetCollider2Ds.Clear();
        }
        
        #endregion
    }
}