using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T i;
    public static T Instance
    {
        get
        {
            if (i == null)
            {
                GameObject gameObject = new GameObject();
                i = gameObject.AddComponent<T>();
                gameObject.name = typeof(T).ToString();
            }
            return i;
        }
    }
}