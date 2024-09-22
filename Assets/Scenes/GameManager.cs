using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public enum PieceType { Tcu1, Tcu2, Tcu3 }

public class GameManager : MonoBehaviour
{
    public List<GameObject> TCUPrefabs; // 動物ピースのプレハブをリストで管理
    public PieceController currentPiece;
    public StageGenerator stageGenerator;// stagegeneratorの参照
    public Button retry; // ゲームオーバーUIのリトライボタン
    public Button title; // ゲームオーバーUIのタイトルボタン
    public Button rotateButton; // ピースを回転させるボタン
    public TextMeshProUGUI gameover; // ゲームオーバーUIのテキスト
    public TextMeshProUGUI MyTurn; // 自分のターンのテキスト
    public TextMeshProUGUI AITurn; // AIのターンのテキスト
    public List<PieceController> allPieces; // すべてのピースを管理するリスト
    public float rotationAngle = 10f; // 一度のクリックで回転する角度
    private GraphicRaycaster raycaster;
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;
    public bool isPlayerTurn = false;
    public float yOffset = 3.5f;
    public Camera mainCamera; // メインカメラの参照

    
    void Start()
    {
        allPieces = new List<PieceController>(); // リストを初期化
        stageGenerator = FindObjectOfType<StageGenerator>();

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
                float scrollInput = Input.GetAxis("Mouse ScrollWheel");
                if (scrollInput != 0f)
                {
                    mainCamera.transform.position += new Vector3(0, scrollInput * 5f, 0); // スクロール量に応じてカメラを移動
                }
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
                float scrollInput = Input.GetAxis("Mouse ScrollWheel");
                if (scrollInput != 0f)
                {
                    mainCamera.transform.position += new Vector3(0, scrollInput * 5f, 0); // スクロール量に応じてカメラを移動
                }
            }
        }
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

        // タワーの高さを取得し、オフセットを追加
        float towerHeight = CalculateTowerHeight();
        if (yOffset - towerHeight < 4.0f)
        {
            yOffset += 2.0f;
            // カメラの位置も更新
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y + 2.0f, mainCamera.transform.position.z);
        }

        Vector3 spawnPosition = new Vector3(0, yOffset, 0);

         // タワーの高さをデバッグ出力
        Debug.Log("Tower Height: " + towerHeight);

        // ピースを生成
        GameObject piece = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
        currentPiece = piece.GetComponent<PieceController>();

        // プレハブのインデックスからピースの種類を設定
        if (randomIndex == 0)
        {
            currentPiece.pieceType = PieceType.Tcu1;
        }
        else if (randomIndex == 1)
        {
            currentPiece.pieceType = PieceType.Tcu2;
        }
        else if (randomIndex == 2)
        {
            currentPiece.pieceType = PieceType.Tcu3;
        }

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

        // yOffsetを元の値に戻す
        yOffset = 3.5f;

        // カメラの位置を初期位置に戻す
        mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, yOffset - 3.5f, mainCamera.transform.position.z);

        // ステージを再生成
        if (stageGenerator != null)
        {
           stageGenerator.GenerateStage();
        }
        else
        {
            Debug.LogError("StageGenerator is null!");
        }

        // 新しいピースを生成する
        SpawnPiece();
        isPlayerTurn = true;

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
    public float CalculateTowerHeight()
    {
        float maxHeight = -6.0f;
        foreach (var piece in allPieces)
        {
            if (piece.transform.position.y > maxHeight)
            {
                maxHeight = piece.transform.position.y;
            }
        }
        return maxHeight;
    }

}
