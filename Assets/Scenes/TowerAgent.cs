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
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // エージェントの行動を処理
        float moveX = actions.ContinuousActions[0]; // ピースの移動
        float rotationZ = actions.ContinuousActions[1]; // ピースの回転

        // ピースを移動および回転させる処理
        MovePiece(moveX);
        RotatePiece(rotationZ);
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
        // ゲーム開始時のリセット処理
        currentPiece.transform.position = new Vector3(0, 5, 0); // 初期位置
        currentPiece.transform.rotation = Quaternion.identity;  // 初期回転
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
}
