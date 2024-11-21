using UnityEngine;

public class BGMController : MonoBehaviour
{
    public AudioSource audioSource;

    public void Start()
    {
        // Play On Awakeで再生されている場合でも制御可能
        if (audioSource.isPlaying)
        {
            Debug.Log("BGMが再生中です");
        }
        else
        {
            Debug.Log("BGMを開始します");
            audioSource.Play();
        }
    }
}
