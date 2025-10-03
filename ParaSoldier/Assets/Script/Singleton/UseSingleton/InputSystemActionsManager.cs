using UnityEngine;
using R3;

namespace Common
{
    /// <summary>
    /// 入力システム管理クラス
    /// InputSystem_Actionsの初期化と有効/無効の制御を行う
    /// </summary>
    public class InputSystemActionsManager : Singleton_MonoBehaviourBase<InputSystemActionsManager>
    {
        // InputSystem_Actionsのインスタンス
        private InputSystem_Actions _InputSystemActions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitializeR3()
        {
            // R3のObservableSystemのFrameProviderを設定
            if (ObservableSystem.DefaultFrameProvider == null)
            {
                ObservableSystem.DefaultFrameProvider = new UnityFrameProvider();
            }
        }
        
        /// <summary>
        /// InputSystem_Actionsのインスタンスを取得する
        /// 初回取得時に自動で初期化される
        /// </summary>
        /// <returns>InputSystem_Actionsのインスタンス</returns>
        public InputSystem_Actions GetInputSystem_Actions()
        {
            if (_InputSystemActions == null)
            {
                _InputSystemActions = new InputSystem_Actions();
            }
            return _InputSystemActions;
        }

        /// <summary>
        /// プレイヤー入力を有効にする
        /// UI入力は無効になる
        /// </summary>
        public void PlayerEnable()
        {
            _InputSystemActions?.Player.Enable();
            _InputSystemActions?.UI.Disable();
        }

        /// <summary>
        /// UI入力を有効にする
        /// プレイヤー入力は無効になる
        /// </summary>
        public void UIEnable()
        {
            _InputSystemActions?.Player.Disable();
            _InputSystemActions?.UI.Enable();
        }

        /// <summary>
        /// プレイヤー入力を無効にする
        /// </summary>
        public void PlayerDisable()
        {
            _InputSystemActions?.Player.Disable();
            Debug.Log("プレイヤー入力無効化");
        }

        /// <summary>
        /// UI入力を無効にする
        /// </summary>
        public void UIDisable()
        {
            _InputSystemActions?.UI.Disable();
            Debug.Log("UI入力無効化");
        }
    }
}