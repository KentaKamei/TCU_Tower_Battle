using UnityEngine;

public class PieceController : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;
    private Rigidbody2D rb;
    private bool isClicked = false;
    private float stationaryTime = 0.0f;
    private float stationaryThreshold = 1.25f; // 速度が一定以下になる時間
    private GameManager gameManager;
    private SpriteRenderer spriteRenderer; // SpriteRendererの参照を追加
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // 最初は重力を無効にする
        rb.velocity = Vector2.zero; // 初期速度をゼロに設定
        gameManager = FindObjectOfType<GameManager>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // SpriteRendererの参照を取得
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

            // ピースが画面外に落下したかどうかをチェック
            if (transform.position.y < -5.0f) // 必要に応じて適切な値に変更
            {
                gameManager.GameOver();
            }
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
}
