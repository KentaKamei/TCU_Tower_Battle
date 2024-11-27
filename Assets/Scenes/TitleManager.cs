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
    public GameObject tcu1;
    public GameObject tcu2;
    public GameObject tcu3;
    public GameObject tcu4;
    public GameObject tcu5;
    public TextMeshProUGUI ClickText; // 変数名を修正

    private bool ObjectsShown = false;
    private bool buttonsShown = false; // ボタンが表示されているかどうかのフラグ
    

    void Start()
    {
        // 最初はボタンを非表示にする
        easy.gameObject.SetActive(false);
        normal.gameObject.SetActive(false);
        hard.gameObject.SetActive(false);
        tcu1.gameObject.SetActive(false);
        tcu2.gameObject.SetActive(false);
        tcu3.gameObject.SetActive(false);
        tcu4.gameObject.SetActive(false);
        tcu5.gameObject.SetActive(false);
    }

    void Update()
    {
        // ボタンが表示されていない場合、クリックで表示する
        if (!ObjectsShown && !buttonsShown && Input.GetMouseButtonDown(0))
        {
            ShowButtons();
            ShowObjects();
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
        void ShowObjects()
    {
        // ボタンを表示する
        tcu1.gameObject.SetActive(true);
        tcu2.gameObject.SetActive(true);
        tcu3.gameObject.SetActive(true);
        tcu4.gameObject.SetActive(true);
        tcu5.gameObject.SetActive(true);

        // フラグを更新して、二度と同じ処理が実行されないようにする
        ObjectsShown = true;
    }
    public void SelectDifficulty(string difficulty)
    {
        GameManager.selectedDifficulty = difficulty; // 難易度を保存
    }

}
