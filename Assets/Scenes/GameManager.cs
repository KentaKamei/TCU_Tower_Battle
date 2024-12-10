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
    public Button rotateButton2; // ピースを回転させるボタン
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
    private float heightRewardThreshold = 3.0f; // 目標とする塔の高さ
    private float previousHighestPoint;
    private float currentHighestPoint;
    private bool isDragging = false; // ドラッグ中かどうかを判定するフラグ
    private Vector3 dragStartPosition; // ドラッグ開始時のマウス位置
    private float pressDuration = 0f; // クリック押下時間
    private float dragThresholdTime = 0.3f; // クリックとドラッグを区別する時間の閾値（秒）
    public AudioSource audioSource;  // 音声再生用AudioSource
    public AudioClip winClip;        // 勝利時の音声
    public AudioClip loseClip;       // 敗北時の音声
    public AudioClip retryClip;       // リトライ時の音声
    private float cameraScrollSpeed = 5.0f; // カメラのスクロール速度
    private float minCameraY = 0.0f; // カメラの最小高さ
    private float maxCameraY = 20.0f; // カメラの最大高さ
    private float clickedTimeThreshold = 15.0f; // ピースがクリックされたままの許容時間（秒）
    private float clickedTimeCounter = 0.0f;   // ピースがクリックされてからの経過時間
    public static string selectedDifficulty = "training"; // デフォルト難易度




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

        Debug.Log("selectedDifficulty:" + selectedDifficulty);

        // ゲームオーバーUIを非表示にする
        retry.gameObject.SetActive(false);
        title.gameObject.SetActive(false);
        win.gameObject.SetActive(false);
        lose.gameObject.SetActive(false);
        MyTurn.gameObject.SetActive(false);
        AITurn.gameObject.SetActive(false);
        
        // 回転ボタンのクリックイベントを設定
        rotateButton.onClick.AddListener(RotatePiece);
        rotateButton2.onClick.AddListener(RotatePiece2);

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
        if (currentPiece != null && !currentPiece.IsClicked)
        {
            // 回転ボタンがnullでない場合にのみ interactable を設定
            if (rotateButton != null)
            {
                rotateButton.interactable = true;
            }
            if (rotateButton2 != null)
            {
                rotateButton2.interactable = true;
            }
        }


        // マウスが押されたとき
        if (Input.GetMouseButtonDown(0) && currentPiece != null)
        {
            // 回転ボタン以外をクリックしたか確認
            if (!IsPointerOverUIElement(rotateButton.gameObject) && !IsPointerOverUIElement(rotateButton2.gameObject))
            {
                if (IsPointerOverPiece(currentPiece)) // カレントピース上でクリックされたらドラッグ開始
                {
                    isDragging = false;
                    pressDuration = 0f; // クリック押下時間をリセット
                    dragStartPosition = Input.mousePosition;
                }
                else // ピース以外をクリックしたら即ドロップ
                {
                    currentPiece.DropPiece();
                    rotateButton.interactable = false;
                    rotateButton2.interactable = false;
                }
            }
        }

        // マウスホイールのスクロール量を取得
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f) // スクロールが発生した場合
        {
            Vector3 cameraPosition = mainCamera.transform.position;

            // スクロール量に基づいてカメラのY位置を変更
            cameraPosition.y += scroll * cameraScrollSpeed;

            // カメラの高さを制限
            cameraPosition.y = Mathf.Clamp(cameraPosition.y, minCameraY, maxCameraY);

            // カメラの新しい位置を適用
            mainCamera.transform.position = cameraPosition;

            Debug.Log($"カメラ位置: {cameraPosition}");
        }


        // マウスが押されている間
        if (Input.GetMouseButton(0) && currentPiece != null)
        {
            // 回転ボタン上ならドラッグ処理を行わない
            if (IsPointerOverUIElement(rotateButton.gameObject) || IsPointerOverUIElement(rotateButton2.gameObject))
            {
                isDragging = false;
                return;
            }

            pressDuration += Time.deltaTime; // 押下時間をカウント

            // ドラッグかどうかの判断
            if (!isDragging && pressDuration > dragThresholdTime)
            {
                isDragging = true; // ドラッグ開始
            }

            if (isDragging && !currentPiece.IsClicked)
            {
                // マウスのドラッグ量に応じてピースを左右に移動
                Vector3 dragCurrentPosition = Input.mousePosition;
                float dragDeltaX = dragCurrentPosition.x - dragStartPosition.x;
                float move = dragDeltaX * 0.0225f; // スケールを調整して適切な移動量に

                // ピースの新しい位置を設定
                Vector3 newPosition = currentPiece.transform.position + new Vector3(move, 0, 0);

                // カメラの右端と左端のワールド座標を取得
                float leftBoundary = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane)).x;
                float rightBoundary = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, Camera.main.nearClipPlane)).x;

                // 新しい位置が画面内に収まるように制限
                newPosition.x = Mathf.Clamp(newPosition.x, leftBoundary, rightBoundary);

                // ピースを新しい位置に移動
                currentPiece.transform.position = newPosition;

                // 次のフレームのために位置を更新
                dragStartPosition = dragCurrentPosition;
            }
        }

        // マウスボタンを離したとき
        if (Input.GetMouseButtonUp(0))
        {
            // ボタン上で離した場合はドラッグやドロップを行わない
            if (IsPointerOverUIElement(rotateButton.gameObject) || IsPointerOverUIElement(rotateButton2.gameObject))
            {
                isDragging = false;
                pressDuration = 0f;
                return;
            }

            if (currentPiece != null && !isDragging && pressDuration <= dragThresholdTime) // 短時間クリックの場合
            {
                currentPiece.DropPiece();
                rotateButton.interactable = false;
                rotateButton2.interactable = false;
            }

            isDragging = false; // ドラッグ状態をリセット
            pressDuration = 0f; // 押下時間をリセット
        }

        // キーボードでの左右移動（ドラッグが発生していない場合のみ）
        if (currentPiece != null && !currentPiece.IsClicked && !isDragging)
        {
            float move = Input.GetAxis("Horizontal") * Time.deltaTime * 7.5f;

            Vector3 newPosition = currentPiece.transform.position + new Vector3(move, 0, 0);

            float leftBoundary = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane)).x;
            float rightBoundary = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, Camera.main.nearClipPlane)).x;

            newPosition.x = Mathf.Clamp(newPosition.x, leftBoundary, rightBoundary);
            currentPiece.transform.position = newPosition;
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

        // トレーニング中でピースがクリックされた状態をチェック
    if (isTrainingMode && currentPiece != null && currentPiece.isClicked)
    {
        // ピースが固定されている場合（通常のトレーニング処理）
        if (currentPiece.IsStationary())
        {
            
            //easy
            
            clickedTimeCounter = 0.0f;
            towerAgent.AddReward(3.0f);
            hasRequestedAction = false;
            Debug.Log("積み上げ成功");
            currentHighestPoint = CalculateTowerHeight(); // ピースドロップ後の高さ

            if(allPieces.Count >= 7)
            {
                towerAgent.AddReward(-10.0f);
                Debug.Log("ピース数10");
            }

            
            //easy

            /*//nomal
            clickedTimeCounter = 0.0f;
            towerAgent.AddReward(5.0f);
            hasRequestedAction = false;
            Debug.Log("積み上げ成功");
            currentHighestPoint = CalculateTowerHeight(); // ピースドロップ後の高さ

            if(allPieces.Count >= 10)
            {
                towerAgent.AddReward(-6.0f);
                Debug.Log("ピース数10");
            }
            //nomal
            */

            //hard
            /*
            clickedTimeCounter = 0.0f;
            towerAgent.AddReward(5.0f);//hard
            hasRequestedAction = false;
            Debug.Log("積み上げ成功");
            currentHighestPoint = CalculateTowerHeight(); // ピースドロップ後の高さ

            if(allPieces.Count == 7)
            {
                towerAgent.AddReward(5.0f);
                Debug.Log("ピース数7");
            }
            if(allPieces.Count == 10)
            {
                towerAgent.AddReward(7.0f);
                Debug.Log("ピース数10");
            }
            if(allPieces.Count == 14)
            {
                towerAgent.AddReward(10.0f);
                Debug.Log("ピース数14");
            }

            
            if (previousHighestPoint - currentPiece.transform.position.y >= 0)
            {
                towerAgent.AddReward(2.0f); // 安定性の報酬
                Debug.Log("安定性の報酬を追加: 2.0f");
            }
            else if (previousHighestPoint - currentPiece.transform.position.y < 0 && currentHighestPoint >= heightRewardThreshold)
            {
                towerAgent.AddReward(3.0f); // 高さ更新の報酬
                Debug.Log("高さ更新の報酬を追加: 5.0f");
            }
            else if (previousHighestPoint - currentPiece.transform.position.y < 0 && currentHighestPoint >= heightRewardThreshold * 2)
            {
                towerAgent.AddReward(5.0f); // 高さ更新の報酬(2)
                Debug.Log("高さ更新の報酬を追加(2): 10.0f");
            }
            else if (previousHighestPoint - currentPiece.transform.position.y < 0 && currentHighestPoint >= heightRewardThreshold * 3)
            {
                towerAgent.AddReward(10.0f); // 高さ更新の報酬(3)
                Debug.Log("高さ更新の報酬を追加(3): 15.0f");
            }
            */
            //hard
        }
        else
        {
            // クリックされ続けている時間をカウント
            clickedTimeCounter += Time.deltaTime;

            // 許容時間を超えた場合にペナルティを与え、次のステップに進む
            if (clickedTimeCounter > clickedTimeThreshold)
            {
                Debug.LogWarning("ピース停止");
                clickedTimeCounter = 0.0f; // タイマーをリセット

                if (currentPiece.rb != null)
                {
                    // 動きを完全に停止
                    currentPiece.rb.velocity = Vector2.zero; // 速度をリセット
                    currentPiece.rb.angularVelocity = 0.0f;  // 回転速度をリセット

                    // 必要に応じて位置固定を有効化
                    currentPiece.rb.constraints = RigidbodyConstraints2D.FreezeAll; // 動きと回転を固定
                    clickedTimeCounter = 0.0f; // タイマーをリセット
                }
                
            }
        }
    }
}
    private bool IsPointerOverPiece(PieceController piece)
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D pieceCollider = piece.GetComponent<Collider2D>();
        return pieceCollider != null && pieceCollider.OverlapPoint(mousePosition);
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
            rotateButton2.interactable = true;
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
            PlaySound(loseClip); // 敗北音声を再生
        }
        else
        {
            win.gameObject.SetActive(true);
            PlaySound(winClip); // 勝利音声を再生
        }

        if (isTrainingMode && towerAgent != null)
        {
            
            //easy
            if(allPieces.Count >= 4 && allPieces.Count < 7)
            {
                towerAgent.AddReward(30.0f);
                towerAgent.EndEpisode(); // エピソード終了
            }
            else
            {
                towerAgent.AddReward(-30.0f);
                towerAgent.EndEpisode(); // エピソード終了
            }
            //easy

            /*//nomal
            if(allPieces.Count >= 7 && allPieces.Count < 10)
            {
                towerAgent.AddReward(30.0f);
                towerAgent.EndEpisode(); // エピソード終了
            }
            else
            {
                towerAgent.AddReward(-20.0f);
                towerAgent.EndEpisode(); // エピソード終了
            }
            *///nomal

            /*
            //hard
            float rewardForStackedPieces = allPieces.Count * 2.0f;
            towerAgent.AddReward(rewardForStackedPieces);
            Debug.Log("ピース数報酬: " + rewardForStackedPieces);
            towerAgent.AddReward(-30.0f); // ペナルティ
            Debug.Log("ペナルティ報酬");
            
            if(allPieces.Count <= 6)//hard
            {
                towerAgent.AddReward(-30.0f);//easy
                towerAgent.EndEpisode(); // エピソード終了
            }
            else if(allPieces.Count <= 10)
            {
                towerAgent.AddReward(-15.0f);//hard
                towerAgent.EndEpisode(); // エピソード終了
            }
            else
            {
                towerAgent.AddReward(20.0f);//hard
                towerAgent.EndEpisode(); // エピソード終了
            }
            */
            //hard

        }
        
        // ゲームの状態を停止またはリセットする処理を追加
        foreach (var piece in allPieces)
        {
            piece.enabled = false;
        }

        // 回転ボタンを無効化
        rotateButton.interactable = false;
        rotateButton2.interactable = false;

        // トレーニング中なら自動でリトライ
        if (isTrainingMode)
        {
            Invoke("Retry", 1.0f); // 1秒後にリトライ（少し待つことで安定）
        }
    }
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip); // 指定された音声を再生
            Debug.Log($"再生中の音声: {clip.name}");
        }
        else
        {
            Debug.LogWarning("AudioSourceまたはAudioClipが設定されていません");
        }
    }

    public void Retry()
    {
        PlaySound(retryClip);
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

    public void RotatePiece2()
    {
        if (currentPiece != null && currentPiece.enabled)
        {
            currentPiece.transform.Rotate(0, 0, -rotationAngle);
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
