using UnityEngine;

public class PersistentBGM : MonoBehaviour
{
    private static PersistentBGM instance;

    private void Awake()
    {
        // インスタンスが既に存在している場合は、このオブジェクトを破棄
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        // インスタンスが存在しない場合は、このオブジェクトを保持
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
