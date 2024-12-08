using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.Barracuda; // NNModel が定義されている名前空間
using UnityEngine;

public class TowerAgent : Agent
{
    public PieceController currentPiece; // 現在のピース
    public GameManager gameManager; // ゲームマネージャーの参照
    public StageGenerator stageGenerator;// stagegeneratorの参照
    public Rigidbody2D currentPieceRigidbody;
    public Transform currentPieceTransform; // Transformをキャッシュする変数
    public BehaviorParameters behaviorParameters;

    public override void Initialize()
    {
        base.Initialize();

        behaviorParameters = GetComponent<BehaviorParameters>();

        // 難易度に応じてモデルを設定
        string difficulty = GameManager.selectedDifficulty;
        Debug.Log("difficultyは" + difficulty + "です");

        switch (difficulty)
        {
            case "easy":
                behaviorParameters.Model = Resources.Load<NNModel>("Models/easy");
                behaviorParameters.BehaviorType = BehaviorType.InferenceOnly;
                Debug.Log("easyモデルがセットされました");
                break;
            case "normal":
                behaviorParameters.Model = Resources.Load<NNModel>("Models/normal");
                behaviorParameters.BehaviorType = BehaviorType.InferenceOnly;
                Debug.Log("normalモデルがセットされました");
                break;
            case "hard":
                behaviorParameters.Model = Resources.Load<NNModel>("Models/hard1");
                behaviorParameters.BehaviorType = BehaviorType.InferenceOnly;
                Debug.Log("hardモデルがセットされました");
                break;
            case "training":
                behaviorParameters.Model = null;
                behaviorParameters.BehaviorType = BehaviorType.Default;
                Debug.Log("training中です");
                break;
        }

        Debug.Log($"Loaded model for difficulty: {difficulty}");
    }


    public override void OnEpisodeBegin()
    {
        // ゲームのリセット処理
        ResetGame();

        if (currentPiece != null)
        {
            currentPieceRigidbody = currentPiece.GetComponent<Rigidbody2D>();
            currentPieceTransform = currentPiece.transform;
        }
        else
        {
            Debug.LogWarning("currentPiece is null after ResetGame.");
        }
        
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
        sensor.AddObservation(currentPiece.transform.position);//space size:3

        // 2. ピースの回転を観測
        sensor.AddObservation(currentPiece.transform.rotation);//space size:4

        // 3. ピースの種類
        sensor.AddObservation((int)currentPiece.pieceType); //space size:1

        // 4. 塔の外形情報（へこみや出っ張り）を観測
        float[] surfaceShape = CalculateSurfaceShape();//space size:15
        foreach (float height in surfaceShape)
        {
            sensor.AddObservation(height);
        }

        // 5. ステージの範囲を観測 (minX, maxX, baseY)//space size:3
        sensor.AddObservation(stageGenerator.minX); // ステージの左端X座標
        sensor.AddObservation(stageGenerator.maxX); // ステージの右端X座標
        sensor.AddObservation(stageGenerator.baseY); // ステージの高さ

        // 6. 現在の全ピース数を観測
        int totalPieces = gameManager.allPieces.Count; // 全ピースの数を取得
        sensor.AddObservation(totalPieces); // space size: 1
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (currentPiece == null)
        {
            Debug.LogWarning("currentPiece is null during action execution.");
            return;
        }

        // エージェントの行動を処理
        float moveX = actions.ContinuousActions[0] * 5; // ピースの移動
        float rotationZ = actions.ContinuousActions[1] * 180; // ピースの回転

        MovePiece(moveX);
        RotatePiece(rotationZ);
        currentPiece.DropPiece();
        //Debug.Log("ドロップピース");

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // 手動での操作をデバッグ用に実装
        //var continuousActionsOut = actionsOut.ContinuousActions;
        //continuousActionsOut[0] = Input.GetAxis("Horizontal");
        //continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    private void ResetGame()
    {
        // すべてのピースを削除
        foreach (PieceController piece in gameManager.allPieces)
        {
            Destroy(piece.gameObject);
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
        if (currentPiece == null) return;

        // 現在のピース位置を取得
        Vector3 newPosition = currentPiece.transform.position + new Vector3(moveX, 0, 0);

        /*//hard
        // ステージの範囲を取得
        float leftBoundary = stageGenerator.minX;
        float rightBoundary = stageGenerator.maxX;

        // 新しい位置がステージの範囲内に収まるように制限
        newPosition.x = Mathf.Clamp(newPosition.x, leftBoundary, rightBoundary);
        *///hard

        // ピースを新しい位置に移動
        currentPiece.transform.position = newPosition;
    }
    private void RotatePiece(float rotationZ)
    {
        if (currentPieceTransform == null) return;

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
}
