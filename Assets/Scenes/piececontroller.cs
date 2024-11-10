using UnityEngine;

public class PieceController : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool isClicked = false;
    private float stationaryTime = 0.0f;
    private float stationaryThreshold = 1.25f; // 速度が一定以下になる時間
    private GameManager gameManager;
    private bool hasFallen = false; // 落下フラグを追加
    public PieceType pieceType;

    
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
            if (rb.velocity.magnitude < 0.0001f)
            {
                stationaryTime += Time.deltaTime;
            }
            else
            {
                stationaryTime = 0.0f; // 動いている間はリセット
            }
        }

        // ピースが画面外に落下したかどうかをチェック
        if (transform.position.y < -8.0f && !hasFallen)
        {
            hasFallen = true;
            gameManager.GameOver(); // 落下を通知
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
            rb.gravityScale = 0.4f; // ピースを落下させる
            isClicked = true;
        }
    }

    public bool HasFallen()
    {
        return hasFallen;
    }
    public bool IsStationary()
    {
        return stationaryTime >= stationaryThreshold;
    }
}
