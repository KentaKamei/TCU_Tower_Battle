using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    public List<GameObject> TCUPrefabs; // 動物ピースのプレハブをリストで管理
    private PieceController currentPiece;
    public Button retry; // ゲームオーバーUIのリトライボタン
    public Button title; // ゲームオーバーUIのタイトルボタン
    public Button rotateButton; // ピースを回転させるボタン
    public TextMeshProUGUI gameover; // ゲームオーバーUIのテキスト
    public TextMeshProUGUI MyTurn; // 自分のターンのテキスト
    public TextMeshProUGUI AITurn; // AIのターンのテキスト
    private List<PieceController> allPieces; // すべてのピースを管理するリスト
    public float rotationAngle = 30f; // 一度のクリックで回転する角度

    private GraphicRaycaster raycaster;
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;
    private bool isPlayerTurn = false;

    void Start()
    {
        allPieces = new List<PieceController>(); // リストを初期化

        if (TCUPrefabs == null || TCUPrefabs.Count == 0)
        {
            Debug.LogError("Piece prefabs are not set.");
        }

        // 初期のピースを生成
        SpawnPiece();

        // ゲームオーバーUIを非表示にする
        retry.gameObject.SetActive(false);
        title.gameObject.SetActive(false);
        gameover.gameObject.SetActive(false);
        MyTurn.gameObject.SetActive(false);
        AITurn.gameObject.SetActive(false);
        
        // 回転ボタンのクリックイベントを設定
        rotateButton.onClick.AddListener(RotatePiece);

        // 必要なコンポーネントを取得
        raycaster = FindObjectOfType<GraphicRaycaster>();
        eventSystem = FindObjectOfType<EventSystem>();
    }

    void Update()
    {
        if (isPlayerTurn)
        {
            MyTurn.gameObject.SetActive(true);
            AITurn.gameObject.SetActive(false);
            if (Input.GetMouseButtonDown(0) && currentPiece != null) // 左クリックが押されたとき
            {
                // 回転ボタン以外のところでクリックされたかチェック
                if (!IsPointerOverUIElement(rotateButton.gameObject))
                {
                    currentPiece.DropPiece();
                    rotateButton.interactable = false;
                }
            }

            if (currentPiece != null && !currentPiece.IsClicked)
            {
                // キーボード入力で左右に移動
                float move = Input.GetAxis("Horizontal") * Time.deltaTime * 5.0f;
                currentPiece.transform.position += new Vector3(move, 0, 0);
            }
        }
        else
        {
            MyTurn.gameObject.SetActive(false);
            AITurn.gameObject.SetActive(true);
            if (Input.GetMouseButtonDown(0) && currentPiece != null) // 左クリックが押されたとき
            {
                // 回転ボタン以外のところでクリックされたかチェック
                if (!IsPointerOverUIElement(rotateButton.gameObject))
                {
                    currentPiece.DropPiece();
                    rotateButton.interactable = false;
                }
            }

            if (currentPiece != null && !currentPiece.IsClicked)
            {
                // キーボード入力で左右に移動
                float move = Input.GetAxis("Horizontal") * Time.deltaTime * 5.0f;
                currentPiece.transform.position += new Vector3(move, 0, 0);
            }
        }
    }

    public void NotifyPieceFell()
    {
        GameOver();
    }

    public void SpawnPiece()
    {
        if (TCUPrefabs == null || TCUPrefabs.Count == 0)
        {
            Debug.LogError("No piece prefabs available to spawn.");
            return;
        }

        // ランダムにプレハブを選択
        int randomIndex = Random.Range(0, TCUPrefabs.Count);
        GameObject selectedPrefab = TCUPrefabs[randomIndex];

        // 指定した位置に生成
        Vector3 spawnPosition = new Vector3(0, 3.5f, 0); // 位置を必要に応じて調整
        GameObject piece = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
        currentPiece = piece.GetComponent<PieceController>();

        // 新しいピースをリストに追加
        allPieces.Add(currentPiece);

        // 回転ボタンを有効化
        rotateButton.interactable = true;
        isPlayerTurn = !isPlayerTurn;
    }

    public void GameOver()
    {
        // ゲームオーバーUIを表示
        retry.gameObject.SetActive(true);
        title.gameObject.SetActive(true);
        gameover.gameObject.SetActive(true);

        // ゲームの状態を停止またはリセットする処理を追加
        foreach (var piece in allPieces)
        {
            piece.enabled = false;
        }

        // 回転ボタンを無効化
        rotateButton.interactable = false;
    }

    public void Retry()
    {
        // ゲームオーバーUIを非表示にする
        retry.gameObject.SetActive(false);
        title.gameObject.SetActive(false);
        gameover.gameObject.SetActive(false);

        // すべてのピースを削除
        foreach (PieceController piece in allPieces)
        {
            Destroy(piece.gameObject);
        }
        allPieces.Clear(); // リストをクリア

        // 新しいピースを生成する
        SpawnPiece();

        // 回転ボタンを有効化
        rotateButton.interactable = true;
    }

    public void BackToTitle()
    {
        // タイトル画面に戻る（"TitleScene"という名前のシーンに切り替え）
        SceneManager.LoadScene("TitleScene");
    }

    public void RotatePiece()
    {
        if (currentPiece != null && currentPiece.enabled)
        {
            currentPiece.transform.Rotate(0, 0, rotationAngle);
        }
    }

    private bool IsPointerOverUIElement(GameObject uiElement)
    {
        pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == uiElement)
            {
                return true;
            }
        }
        return false;
    }
}
