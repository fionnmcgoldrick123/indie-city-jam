using UnityEngine;

public class GenericSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();

                if (_instance == null)
                {
                    Debug.LogError($"{typeof(T).Name} singleton was requested but no instance exists in the scene.");
                    return null;
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject); // only if you truly want persistence
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"Multiple instances of {typeof(T).Name} detected. Destroying {name}.");
            Destroy(gameObject);
        }
    }
}