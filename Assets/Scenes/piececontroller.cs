using UnityEngine;

public class PieceController : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;
    private Rigidbody2D rb;
    private bool isClicked = false;
    private float stationaryTime = 0.0f;
    private float stationaryThreshold = 1.0f; // 速度が一定以下になる時間
    private GameManager gameManager;
    
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
            if (rb.velocity.magnitude < 0.1f)
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
