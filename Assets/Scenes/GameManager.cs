using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Unity.MLAgents; 

public enum PieceType { Tcu1, Tcu2, Tcu3, Tcu4, Tcu5 }

public class GameManager : MonoBehaviour
{
    public List<GameObject> TCUPrefabs; // 動物ピースのプレハブをリストで管理
    public PieceController currentPiece;
    public StageGenerator stageGenerator;// stagegeneratorの参照
    public Button retry; // ゲームオーバーUIのリトライボタン
    public Button title; // ゲームオーバーUIのタイトルボタン
    public Button rotateButton; // ピースを回転させるボタン
    public TextMeshProUGUI win; // ゲームオーバーUIのテキスト
    public TextMeshProUGUI lose; // ゲームオーバーUIのテキスト
    public TextMeshProUGUI MyTurn; // 自分のターンのテキスト
    public TextMeshProUGUI AITurn; // AIのターンのテキスト
    public List<PieceController> allPieces; // すべてのピースを管理するリスト
    public float rotationAngle = 10f; // 一度のクリックで回転する角度
    private GraphicRaycaster raycaster;
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;
    public bool isPlayerTurn = true;
    public float yOffset = 3.5f;
    public Camera mainCamera; // メインカメラの参照
    public bool isTrainingMode = false; // トレーニングモードかどうかを判断するフラグ
    public TowerAgent towerAgent; // TowerAgentの参照
    public bool hasRequestedAction = false; // 行動要求のフラグ
    private float heightRewardThreshold = 5.0f; // 目標とする塔の高さ
    private float previousHighestPoint;
    private float currentHighestPoint;
    
    void Start()
    {
        if (allPieces == null)
        {
            allPieces = new List<PieceController>(); // リストがnullのときのみ初期化
        }
        stageGenerator = FindObjectOfType<StageGenerator>();
        towerAgent = FindObjectOfType<TowerAgent>();
        isPlayerTurn = true;

        // ML-Agentsがトレーニング中かどうかを確認
        isTrainingMode = Academy.Instance.IsCommunicatorOn;

        if (TCUPrefabs == null || TCUPrefabs.Count == 0)
        {
            Debug.LogError("Piece prefabs are not set.");
        }

        // ゲームオーバーUIを非表示にする
        retry.gameObject.SetActive(false);
        title.gameObject.SetActive(false);
        win.gameObject.SetActive(false);
        lose.gameObject.SetActive(false);
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
        // トレーニング時は両方AIが行動
        if (isTrainingMode)
        {
            HandleAITurn();  // 両方のターンでAIが行動する
        }
        else
        {
            if (isPlayerTurn)
            {
                HandlePlayerTurn();  // プレイヤーのターン処理
            }
            else
            {
                HandleAITurn();  // AIのターン処理
            }
        }
        
        if (allPieces.Count > 0 && AreAllPiecesStationary())
        {
            SpawnPiece();
        }


    }


    public bool AreAllPiecesStationary()
    {
        foreach (var piece in allPieces)
        {
            if (!piece.IsStationary())
            {
                return false;
            }
        }
        return true;
    }
    private void HandlePlayerTurn()
    {
        MyTurn.gameObject.SetActive(true);
        AITurn.gameObject.SetActive(false);
        //towerAgent.SetPieceVisible(true);

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

            // 現在のピース位置を取得
            Vector3 newPosition = currentPiece.transform.position + new Vector3(move, 0, 0);

            // カメラの右端と左端のワールド座標を取得
            float leftBoundary = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane)).x; // 画面の左端
            float rightBoundary = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, Camera.main.nearClipPlane)).x; // 画面の右端

            // 新しい位置が画面内に収まるように制限
            newPosition.x = Mathf.Clamp(newPosition.x, leftBoundary, rightBoundary);

            // ピースを新しい位置に移動
            currentPiece.transform.position = newPosition;

            currentPiece.transform.position += new Vector3(move, 0, 0);
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0f)
            {
                mainCamera.transform.position += new Vector3(0, scrollInput * 5f, 0); // スクロール量に応じてカメラを移動
            }
        }
    }

    // AIのターンの処理
    private void HandleAITurn()
    {
        MyTurn.gameObject.SetActive(false);
        AITurn.gameObject.SetActive(true);


        // 現在のピースが存在し、まだ落下していない場合のみAIに行動をリクエスト
        if (towerAgent != null && currentPiece != null && !currentPiece.IsClicked)
        {
            if (!hasRequestedAction)
            {
                previousHighestPoint = CalculateTowerHeight();//ピースドロップ前の高さ
                towerAgent.RequestDecision();
                hasRequestedAction = true;
            }
        }

        if(isTrainingMode && currentPiece.IsStationary() && currentPiece.isClicked)
        {
            towerAgent.AddReward(5.0f);
            hasRequestedAction = false;
            Debug.Log("積み上げ成功");
            currentHighestPoint = CalculateTowerHeight();//ピースドロップ後の高さ

            if (previousHighestPoint - currentPiece.transform.position.y >= 0)
            {
                towerAgent.AddReward(1.0f); // 安定性の報酬
                Debug.Log("安定性の報酬を追加: 1.0f");
            }
            else if (previousHighestPoint - currentPiece.transform.position.y < 0 && currentHighestPoint >= heightRewardThreshold)
            {
                towerAgent.AddReward(5.0f); // 安定性の報酬
                Debug.Log("高さ更新の報酬を追加: 5.0f");
            }
            else if (previousHighestPoint - currentPiece.transform.position.y < 0 && currentHighestPoint >= heightRewardThreshold * 2)
            {
                towerAgent.AddReward(10.0f); // 安定性の報酬
                Debug.Log("高さ更新の報酬を追加(2): 10.0f");
            }
            else if (previousHighestPoint - currentPiece.transform.position.y < 0 && currentHighestPoint >= heightRewardThreshold * 3)
            {
                towerAgent.AddReward(15.0f); // 安定性の報酬
                Debug.Log("高さ更新の報酬を追加(3): 15.0f");
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

        Debug.Log("spawn");

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
        else if (randomIndex == 3)
        {
            currentPiece.pieceType = PieceType.Tcu4;
        }
        else if (randomIndex == 4)
        {
            currentPiece.pieceType = PieceType.Tcu5;
        }

        // 新しいピースをリストに追加
        allPieces.Add(currentPiece);

         if (towerAgent != null)
        {
            towerAgent.currentPiece = currentPiece;
            towerAgent.currentPieceRigidbody = currentPiece.GetComponent<Rigidbody2D>();
            towerAgent.currentPieceTransform = currentPiece.transform;
            //Debug.Log("TowerAgent's currentPiece updated to: " + currentPiece.name);
        }


        isPlayerTurn = !isPlayerTurn;
        if(isPlayerTurn)
        {
            // 回転ボタンを有効化
            rotateButton.interactable = true;
        }
        else
        {
            hasRequestedAction = false;
        }
        
    }

    public void GameOver()
    {
        // ゲームオーバーUIを表示
        retry.gameObject.SetActive(true);
        title.gameObject.SetActive(true);

        if(isPlayerTurn)
        {
            lose.gameObject.SetActive(true);
        }
        else
        {
            win.gameObject.SetActive(true);
        }

        if (isTrainingMode && towerAgent != null)
        {
            float rewardForStackedPieces = allPieces.Count * 2.0f;
            towerAgent.AddReward(rewardForStackedPieces);
            Debug.Log("ピース数報酬: " + rewardForStackedPieces);

            towerAgent.AddReward(-20.0f); // ペナルティ
            Debug.Log("ペナルティ報酬");

            towerAgent.EndEpisode(); // エピソード終了
        }
        
        // ゲームの状態を停止またはリセットする処理を追加
        foreach (var piece in allPieces)
        {
            piece.enabled = false;
        }

        // 回転ボタンを無効化
        rotateButton.interactable = false;

        // トレーニング中なら自動でリトライ
        if (isTrainingMode)
        {
            Invoke("Retry", 1.0f); // 1秒後にリトライ（少し待つことで安定）
        }
    }

    public void Retry()
    {
        // ゲームオーバーUIを非表示にする
        retry.gameObject.SetActive(false);
        title.gameObject.SetActive(false);
        win.gameObject.SetActive(false);
        lose.gameObject.SetActive(false);

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
        hasRequestedAction = false;

        // 回転ボタンを有効化
        rotateButton.interactable = true;

    }

    public void BackToTitle()
    {
        // すべてのピースを削除しリストをクリア
        foreach (PieceController piece in allPieces)
        {
            Destroy(piece.gameObject);
        }
        allPieces.Clear();

        // ゲームの状態をリセット
        towerAgent.currentPiece = null;
        towerAgent.currentPieceRigidbody = null;
        towerAgent.currentPieceTransform = null;

        // 初期位置やタイマーのリセット
        currentPiece = null;
        yOffset = 3.5f;
        isPlayerTurn = true;
        hasRequestedAction = false;

        // カメラ位置を初期位置に戻す
        mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, yOffset - 3.5f, mainCamera.transform.position.z);


        // タイトル画面へ遷移
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
        float maxHeight = stageGenerator.baseY;
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
