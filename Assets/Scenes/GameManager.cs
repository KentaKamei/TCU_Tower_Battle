using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public List<GameObject> TCUPrefabs; // 動物ピースのプレハブをリストで管理
    private PieceController currentPiece;

    void Start()
    {
        if (TCUPrefabs == null || TCUPrefabs.Count == 0)
        {
            Debug.LogError("Piece prefabs are not set.");
        }

        // 初期のピースを生成
        SpawnPiece();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 左クリックが押されたとき
        {
            currentPiece.DropPiece();
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

        // 指定した位置に生成
        Vector3 spawnPosition = new Vector3(0, 3.5f, 0); // 位置を必要に応じて調整
        GameObject piece = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
        currentPiece = piece.GetComponent<PieceController>();
    }
}
