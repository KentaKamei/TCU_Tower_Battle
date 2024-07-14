using UnityEngine;

public class PieceController : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool isClicked = false;
    private float stationaryTime = 0.0f;
    private float stationaryThreshold = 1.25f; // 速度が一定以下になる時間
    private GameManager gameManager;
    private bool hasFallen = false; // 落下フラグを追加
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // 最初は重力を無効にする
        rb.velocity = Vector2.zero; // 初期速度をゼロに設定
        gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        if (isClicked)
        {
            // 速度が一定以下かどうかをチェック
            if (rb.velocity.magnitude < 0.00001f)
            {
                stationaryTime += Time.deltaTime;
                if (stationaryTime >= stationaryThreshold)
                {
                    // 一定時間静止したら次のピースを生成
                    gameManager.SpawnPiece();
                    isClicked = false;
                }
            }
            else
            {
                stationaryTime = 0.0f; // 動いている間はリセット
            }
        }
        // ピースが画面外に落下したかどうかをチェック
        if (transform.position.y < -8.0f && !hasFallen) // 必要に応じて適切な値に変更
        {
            hasFallen = true;
            gameManager.NotifyPieceFell(); // 落下を通知
        }
    }

    public bool IsClicked
    {
        get { return isClicked; }
    }

    public void DropPiece()
    {
        if (!isClicked)
        {
            rb.gravityScale = 0.5f; // ピースを落下させる
            isClicked = true;
        }
    }

    public bool HasFallen()
    {
        return hasFallen;
    }
}
