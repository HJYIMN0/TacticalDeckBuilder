using UnityEngine;

public abstract class GenericSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static bool isApplicationQuitting = false;

    public abstract bool IsDestroyedOnLoad();
    public abstract bool ShouldDetatchFromParent();

    public static bool HasInstance => instance != null;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType(typeof(T)) as T;
                if (instance == null && !isApplicationQuitting)
                {
                    GameObject go = new GameObject(typeof(T).Name + "_Singleton");
                    instance = go.AddComponent<T>();
                    Debug.Log($"[Singleton] Creato {go.name}");
                }
            }
            return instance;
        }
    }

    public virtual void Awake()
    {
        if (instance == null)
        {
            if (ShouldDetatchFromParent()) transform.parent = null;
            instance = GetComponent<T>();
            if (!IsDestroyedOnLoad()) DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }
}