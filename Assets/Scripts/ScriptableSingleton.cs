using UnityEngine;

public abstract class ScriptableSingleton<T> : ScriptableObject where T : ScriptableObject
{

    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<T>(typeof(T).ToString());
                (_instance as ScriptableSingleton<T>).OnInitialize();
            }
            return _instance;
        }
    }

    protected virtual void OnInitialize() { }
}
