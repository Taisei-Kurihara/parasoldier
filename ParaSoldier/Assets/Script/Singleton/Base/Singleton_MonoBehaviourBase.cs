using UnityEngine;


namespace Common
{
    public class Singleton_MonoBehaviourBase<T> : MonoBehaviour where T : Singleton_MonoBehaviourBase<T>
    {
        protected static T instance;

        public static T Instance()
        {
            if (instance == null)
            {
                var gameObject = new GameObject(typeof(T).Name);
                instance = gameObject.AddComponent<T>();
                DontDestroyOnLoad(gameObject);
            }
            return instance;
        }
    }
}
