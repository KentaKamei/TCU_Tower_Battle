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
    private float noMovementThreshold = 1.0f; // ピースが動かなくなってから落下させるまでの時間（秒）
    public Transform currentPieceTransform; // Transformをキャッシュする変数
    private bool isVisible;

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

        // ステージの形状も観測に追加
        float[] stageShape = CalculateStageShape();
        foreach (float shape in stageShape)
        {
            sensor.AddObservation(shape);
        }

    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        // すでにピースが落下していたら行動しない
        if (currentPiece.IsClicked)
        {
            return;
        }

        // エージェントの行動を処理
        float moveX = actions.ContinuousActions[0]; // ピースの移動
        float rotationZ = actions.ContinuousActions[1]; // ピースの回転

        // ピースを移動および回転させる処理
        MovePiece(moveX);
        RotatePiece(rotationZ);

        // 動かした後、ピースの速度と角速度を確認
        if (currentPieceRigidbody.velocity.magnitude < 0.01f && Mathf.Abs(currentPieceRigidbody.angularVelocity) < 0.1f)
        {
            noMovementTime += Time.deltaTime;
            
            // 一定時間が経過した場合にピースを落下させる
            if (noMovementTime >= noMovementThreshold)
            {
                SetPieceVisible(true);
                currentPiece.DropPiece();
                //Debug.Log("ドロップピース");
            }
        }
        else
        {
            noMovementTime = 0.0f; // ピースが動いている間はリセット
        }

        // すべてのピースをチェック
        foreach (var piece in gameManager.allPieces)
        {
            PieceController pieceController = piece.GetComponent<PieceController>();
            if (pieceController.HasFallen())
            {
                //Debug.Log("ピースが落下しました。エピソード終了。");
                AddReward(-5.0f); // ペナルティ
                EndEpisode(); // エピソード終了
                return;
            }
        }

        //Debug.Log("エピソード継続中。報酬を追加。");
        AddReward(0.5f); // ピースがまだ落ちていないなら報酬

        float towerHeight = gameManager.CalculateTowerHeight();
        AddReward(towerHeight * 0.05f);
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

        // ステージの幅に基づく移動制限
        float leftBoundary = -stageGenerator.totalWidth / 2;  // ステージの左端
        float rightBoundary = stageGenerator.totalWidth / 2;  // ステージの右端

        // 新しい位置がステージ内に収まるように制限
        newPosition.x = Mathf.Clamp(newPosition.x, leftBoundary, rightBoundary);

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

    private float[] CalculateStageShape()
    {
        if (cachedStageShape != null) return cachedStageShape;

        // stageGeneratorの参照があることを確認
        if (stageGenerator == null)
        {
            Debug.LogError("StageGenerator is null!");
            return new float[0]; // 空の配列を返す
        }

        // MeshFilterからメッシュを取得
        Mesh mesh = stageGenerator.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        // 頂点のY座標を高さ情報として取得
        cachedStageShape = new float[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            cachedStageShape[i] = vertices[i].y; // Y軸方向の高さを観測
        }

        return cachedStageShape;
    }

    public void SetPieceVisible(bool isVisible)
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
