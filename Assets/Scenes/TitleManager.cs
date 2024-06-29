using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshProの名前空間を追加

public class TitleManager : MonoBehaviour
{
    public Button easy;
    public Button normal;
    public Button hard;
    public TextMeshProUGUI ClickText; // 変数名を修正


    private bool buttonsShown = false; // ボタンが表示されているかどうかのフラグ

    // Start is called before the first frame update
    void Start()
    {
        // 最初はボタンを非表示にする
        easy.gameObject.SetActive(false);
        normal.gameObject.SetActive(false);
        hard.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // ボタンが表示されていない場合、クリックで表示する
        if (!buttonsShown && Input.GetMouseButtonDown(0))
        {
            ShowButtons();
            ClickText.gameObject.SetActive(false);
        }
    }

    void ShowButtons()
    {
        // ボタンを表示する
        easy.gameObject.SetActive(true);
        normal.gameObject.SetActive(true);
        hard.gameObject.SetActive(true);

        // フラグを更新して、二度と同じ処理が実行されないようにする
        buttonsShown = true;
    }
}
