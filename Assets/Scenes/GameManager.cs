using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public List<GameObject> TCUPrefabs; // 動物ピースのプレハブをリストで管理
    private PieceController currentPiece;
    public Button retry; // ゲームオーバーUIのリトライボタン
    public Button title; // ゲームオーバーUIのタイトルボタン
    public TextMeshProUGUI gameover; // ゲームオーバーUIのテキスト
    private List<GameObject> allPieces; // すべてのピースを管理するリスト

    void Start()
    {
        allPieces = new List<GameObject>(); // リストを初期化

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
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && currentPiece != null) // 左クリックが押されたとき
        {
            currentPiece.DropPiece();
        }

        if (currentPiece != null && !currentPiece.IsClicked)
        {
            // キーボード入力で左右に移動
            float move = Input.GetAxis("Horizontal") * Time.deltaTime * 5.0f;
            currentPiece.transform.Translate(move, 0, 0);
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

        // 新しいピースをリストに追加
        allPieces.Add(piece);
    }

    public void GameOver()
    {
        // ゲームオーバーUIを表示
        retry.gameObject.SetActive(true);
        title.gameObject.SetActive(true);
        gameover.gameObject.SetActive(true);

        // ゲームの状態を停止またはリセットする処理を追加
        // 例えば、すべてのピースの動きを停止させるなど
        if (currentPiece != null)
        {
            currentPiece.enabled = false;
        }
    }

    public void Retry()
    {
        // ゲームオーバーUIを非表示にする
        retry.gameObject.SetActive(false);
        title.gameObject.SetActive(false);
        gameover.gameObject.SetActive(false);

        // すべてのピースを削除
        foreach (GameObject piece in allPieces)
        {
            Destroy(piece);
        }
        allPieces.Clear(); // リストをクリア

        // 新しいピースを生成する
        SpawnPiece();
    }

    public void BackToTitle()
    {
        // タイトル画面に戻る（"TitleScene"という名前のシーンに切り替え）
        SceneManager.LoadScene("TitleScene");
    }
}
