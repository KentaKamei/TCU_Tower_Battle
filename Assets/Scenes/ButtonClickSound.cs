using UnityEngine;
using UnityEngine.UI;

public class ButtonClickSound : MonoBehaviour
{
    public AudioSource audioSource; // AudioSourceをアタッチ
    public AudioClip clickSound;    // 再生する音

    void Start()
    {
        // ボタンコンポーネントを取得
        Button button = GetComponent<Button>();
        if (button != null)
        {
            // PlayClickSoundをOnClickイベントに登録
            button.onClick.AddListener(PlayClickSound);
            Debug.Log("PlayClickSoundをOnClickイベントに追加しました");
        }
        else
        {
            Debug.LogWarning("Buttonコンポーネントが見つかりません");
        }
    }

    public void PlayClickSound()
    {
        Debug.Log("PlayClickSoundが呼び出されました");
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
            Debug.Log("クリック音再生");
        }
        else
        {
            Debug.LogWarning("AudioSourceまたはAudioClipが設定されていません");
        }
    }
}
