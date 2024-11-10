using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class TowerAgent : Agent
{
    public PieceController currentPiece; // 現在のピース
    public GameManager gameManager; // ゲームマネージャーの参照
    public StageGenerator stageGenerator;// stagegeneratorの参照
    public Rigidbody2D currentPieceRigidbody;
    private float[] cachedStageShape;
    private float noMovementTime = 0.0f; // ピースが動かなくなってからの経過時間
    private float noMovementThreshold = 3.0f; // ピースが動かなくなってから落下させるまでの時間（秒）
    public Transform currentPieceTransform; // Transformをキャッシュする変数
    private bool isVisible;
    private float movementThreshold = 0.1f; // 小さな動きを無視する閾値
    private float totalMoveX = 0.0f;
    private float totalRotationX = 0.0f;
    private float baceline = 75f;



    public override void OnEpisodeBegin()
    {
        // ゲームのリセット処理
        ResetGame();
        ResetStageCache();  // キャッシュをクリア
        currentPieceRigidbody = currentPiece.GetComponent<Rigidbody2D>(); 
        currentPieceTransform = currentPiece.transform; 
        gameManager.isPlayerTurn = true; // プレイヤーのターンからスタート
        
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (currentPiece == null)
        {
            Debug.LogWarning("currentPiece is null during observation collection.");
            return;
        }
        // 1. ピースの位置を観測
        sensor.AddObservation(currentPiece.transform.position);

        // 2. ピースの回転を観測
        sensor.AddObservation(currentPiece.transform.rotation);
        
        // 3. ピースの種類
        sensor.AddObservation((int)currentPiece.pieceType); 

        // 4. 塔の外形情報（へこみや出っ張り）を観測
        float[] surfaceShape = CalculateSurfaceShape();
        foreach (float height in surfaceShape)
        {
            sensor.AddObservation(height);
        }

        // 5. ステージの範囲を観測 (minX, maxX, baseY)
        sensor.AddObservation(stageGenerator.minX); // ステージの左端X座標
        sensor.AddObservation(stageGenerator.maxX); // ステージの右端X座標
        sensor.AddObservation(stageGenerator.baseY); // ステージの高さ
    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        // エージェントの行動を処理
        float moveX = actions.ContinuousActions[0]; // ピースの移動
        float rotationZ = actions.ContinuousActions[1] * 60; // ピースの回転

        // ピースを移動および回転させる処理
        if (Mathf.Abs(moveX) > movementThreshold)
        {
            MovePiece(moveX);
            totalMoveX += Mathf.Abs(moveX);
        }
        if (Mathf.Abs(actions.ContinuousActions[1]) > movementThreshold)
        {
            RotatePiece(rotationZ);
            totalRotationX += Mathf.Abs(actions.ContinuousActions[1]);
        }


        
        // 動かした後、ピースの速度と角速度を確認
        if (currentPieceRigidbody.velocity.magnitude < 0.01f && Mathf.Abs(currentPieceRigidbody.angularVelocity) < 0.1f)
        {
            noMovementTime += Time.deltaTime;
            
            // 一定時間が経過した場合にピースを落下させる
            if (noMovementTime >= noMovementThreshold)
            {
                SetPieceVisible(true);
                currentPiece.DropPiece();
                Debug.Log("ドロップピース");
                noMovementTime = 0.0f;
                if(totalMoveX + totalRotationX > baceline)
                {
                    AddReward(totalMoveX + totalRotationX - baceline);
                    Debug.Log("動きすぎ" + (totalMoveX + totalRotationX - baceline));
                }
                else
                {
                    AddReward(baceline - totalMoveX + totalRotationX);
                    Debug.Log("スムーズ" + (baceline - totalMoveX + totalRotationX));
                }
                totalMoveX = 0;
                totalRotationX = 0;

            }

        }
        else
        {
            noMovementTime = 0.0f; // ピースが動いている間はリセット
        }

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // 手動での操作をデバッグ用に実装
        //var continuousActionsOut = actionsOut.ContinuousActions;
        //continuousActionsOut[0] = Input.GetAxis("Horizontal");
        //continuousActionsOut[1] = Input.GetAxis("Vertical");
    }


    // ステージが再生成されるたびにキャッシュをクリアする
    private void ResetStageCache()
    {
        cachedStageShape = null;  // キャッシュをリセット
    }

    private void ResetGame()
    {
        // すべてのピースを削除
        foreach (PieceController piece in gameManager.allPieces)
        {
            Destroy(piece.gameObject);
            ResetStageCache();  // キャッシュをクリア
        }
        gameManager.allPieces.Clear(); // リストをクリア


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
        gameManager.SpawnPiece();
        currentPiece = gameManager.currentPiece;
        currentPieceRigidbody = currentPiece.GetComponent<Rigidbody2D>(); 
        gameManager.isPlayerTurn = true;

    }

    private void MovePiece(float moveX)
    {
        // 現在のピース位置を取得
        Vector3 newPosition = currentPiece.transform.position + new Vector3(moveX, 0, 0);

        // ステージの範囲を取得
        float leftBoundary = stageGenerator.minX;
        float rightBoundary = stageGenerator.maxX;
        //Debug.Log("leftBoundary" + leftBoundary);
        //Debug.Log("rightBoundary" + rightBoundary);


        // 新しい位置がステージの範囲内に収まるように制限
        newPosition.x = Mathf.Clamp(newPosition.x, leftBoundary, rightBoundary);
        //Debug.Log("newPosition.x" + newPosition.x);


        // ピースを新しい位置に移動
        currentPiece.transform.position = newPosition;
    }

    private void RotatePiece(float rotationZ)
    {
        // ピースを回転させる処理
        currentPieceTransform.Rotate(new Vector3(0, 0, rotationZ));

    }


    // 塔の形
    private float[] CalculateSurfaceShape()
    {
        int segments = 15; // 塔を15分割して計算
        float[] surfaceShape = new float[segments];

        // StageGeneratorのtotalWidthを使用
        float totalWidth = stageGenerator.totalWidth;

        foreach (var piece in gameManager.allPieces)
        {
            int index = Mathf.FloorToInt((piece.transform.position.x + totalWidth / 2) / totalWidth * segments); 
            if (index >= 0 && index < segments)
            {
                surfaceShape[index] = Mathf.Max(surfaceShape[index], piece.transform.position.y);
            }
        }

        return surfaceShape;
    }


    public void SetPieceVisible(bool isVisible)
    {
        if (currentPiece != null)
        {
            // SpriteRendererがある場合
            SpriteRenderer spriteRenderer = currentPiece.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = isVisible;
            }

            // MeshRendererがある場合（3Dモデル用）
            MeshRenderer meshRenderer = currentPiece.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = isVisible;
            }
        }
    }


}
