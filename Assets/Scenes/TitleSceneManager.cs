using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TitleScreenManager : MonoBehaviour
{
    public float delayBeforeSceneLoad = 3.0f; // シーン切り替えの遅延時間

    public void StartGame()
    {
        StartCoroutine(LoadSceneWithDelay());
    }

    private IEnumerator LoadSceneWithDelay()
    {
        Debug.Log("遅延開始: " + delayBeforeSceneLoad + " 秒");
        yield return new WaitForSeconds(delayBeforeSceneLoad); // 指定した秒数待機
        SceneManager.LoadScene("GameScene");
        Debug.Log("シーン切り替え完了");
    }
}
