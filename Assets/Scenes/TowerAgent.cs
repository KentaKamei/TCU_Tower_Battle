using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class TowerAgent : Agent
{
    public PieceController currentPiece; // 現在のピース
    public GameManager gameManager; // ゲームマネージャーの参照
    public StageGenerator stageGenerator;// stagegeneratorの参照

    public override void OnEpisodeBegin()
    {
        // ゲームのリセット処理
        ResetGame();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. ピースの位置を観測
        sensor.AddObservation(currentPiece.transform.position);

        // 2. ピースの回転を観測
        sensor.AddObservation(currentPiece.transform.rotation);
        
        // 3. ピースの安定性を観測（速度や回転速度で判断）
        sensor.AddObservation(currentPiece.GetComponent<Rigidbody2D>().velocity);
        sensor.AddObservation(currentPiece.GetComponent<Rigidbody2D>().angularVelocity);

        // 4. 塔の外形情報（へこみや出っ張り）を観測
        float[] heightMap = CalculateHeightMap();
        foreach (float height in heightMap)
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
        // エージェントの行動を処理
        float moveX = actions.ContinuousActions[0]; // ピースの移動
        float rotationZ = actions.ContinuousActions[1]; // ピースの回転

        // ピースを移動および回転させる処理
        MovePiece(moveX);
        RotatePiece(rotationZ);

        // ピースを落下させる
        currentPiece.DropPiece();

        // すべてのピースをチェック
        foreach (var piece in gameManager.allPieces)
        {
            PieceController pieceController = piece.GetComponent<PieceController>();
            if (pieceController.HasFallen())
            {
                Debug.Log("ピースが落下しました。エピソード終了。");
                AddReward(-5.0f); // ペナルティ
                EndEpisode(); // エピソード終了
                return;
            }
        }
        Debug.Log("エピソード継続中。報酬を追加。");
        AddReward(0.5f); // ピースがまだ落ちていないなら報酬

        float towerHeight = gameManager.CalculateTowerHeight();
        AddReward(towerHeight * 0.05f);

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // 手動での操作をデバッグ用に実装
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
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
    gameManager.isPlayerTurn = true;

}

    private void MovePiece(float moveX)
    {
        // ピースを左右に移動する処理
        currentPiece.transform.position += new Vector3(moveX, 0, 0);
    }

    private void RotatePiece(float rotationZ)
    {
        // ピースを回転させる処理
        currentPiece.transform.Rotate(new Vector3(0, 0, rotationZ));

    }


    // 塔の高さマップ（へこみや出っ張りの計算）
    private float[] CalculateHeightMap()
    {
        int heightSegments = 15; // 塔を10分割して計算
        float[] heightMap = new float[heightSegments];

        foreach (var piece in gameManager.allPieces)
        {
            int heightIndex = Mathf.FloorToInt(piece.transform.position.y - stageGenerator.baseY); // 高さを整数に丸める
            if (heightIndex >= 0 && heightIndex < heightSegments)
            {
                heightMap[heightIndex] += 1; // 各高さにピースの数を追加
            }
        }

        return heightMap;
    }
    private float[] CalculateStageShape()
    {
        // stageGeneratorの参照があることを確認
        if (stageGenerator == null)
        {
            Debug.LogError("StageGenerator is null!");
            return new float[0]; // 空の配列を返す
        }

        // StageGeneratorオブジェクトからMeshFilterを取得
        Mesh mesh = stageGenerator.GetComponent<MeshFilter>().mesh;
        
        // メッシュの頂点を取得
        Vector3[] vertices = mesh.vertices;

        // 頂点のY座標を高さ情報として取得
        float[] stageShape = new float[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            stageShape[i] = vertices[i].y; // Y軸方向の高さを観測
        }

    return stageShape;
    }


}
