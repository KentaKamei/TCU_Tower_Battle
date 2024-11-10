using UnityEngine;
using UnityEngine.Rendering;

public class StageGenerator : MonoBehaviour
{
    public int minTriangles = 7;
    public int maxTriangles = 11;
    public float maxWidth = 1.3f;
    public float minHeight = 0.5f;
    public float maxHeight = 0.7f;
    public float baseY = -6.0f; // 三角形の底辺のy座標
    public float overlapFactor = 0.3f; // 重なりの度合い
    public Material stageMaterial; // ステージ用のマテリアル
    public float totalWidth; // ステージ全体の幅
    public float minX; // ステージのX座標範囲の最小値
    public float maxX; // ステージのX座標範囲の最大値


    void Start()
    {
        // アンチエイリアシングを4xに設定
        QualitySettings.antiAliasing = 4;
        GenerateStage();
    }

    public void GenerateStage()
    {

        // 既存のMeshFilterを取得し、古いメッシュを破棄
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        else
        {
            Destroy(meshFilter.mesh);  // 古いメッシュを破棄
        }

        Mesh mesh = new Mesh();

        int triangleCount = Random.Range(minTriangles, maxTriangles);
        Vector3[] vertices = new Vector3[triangleCount * 3];
        int[] triangles = new int[triangleCount * 3];

        // 初期X座標を計算して調整
        float currentX = -0.75f;

        for (int i = 0; i < triangleCount; i++)
        {
            float height = Random.Range(minHeight, maxHeight);
            float width = Random.Range(maxWidth / 2, maxWidth);

            // 三角形の頂点を設定
            vertices[i * 3] = new Vector3(currentX + width, baseY, 0); // 右上
            vertices[i * 3 + 1] = new Vector3(currentX + width / 2, baseY - height, 0); // 下
            vertices[i * 3 + 2] = new Vector3(currentX, baseY, 0); // 左上

            // X座標の最小値と最大値を更新
            minX = Mathf.Min(minX, vertices[i * 3 + 2].x);
            maxX = Mathf.Max(maxX, vertices[i * 3].x);
            
            // 三角形の頂点インデックスを設定
            triangles[i * 3] = i * 3;
            triangles[i * 3 + 1] = i * 3 + 1;
            triangles[i * 3 + 2] = i * 3 + 2;

            // 次の三角形の位置を更新（少し重ねる）
            currentX += width * (1 - overlapFactor); // 重なりの度合いを調整する
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        // メッシュの幅を計算して保存
        totalWidth = Mathf.Abs(vertices[0].x - vertices[vertices.Length - 1].x); // ステージの幅を計算

        // ステージの中心を計算してオフセット
        float stageCenterX = CalculateStageCenterX(vertices, overlapFactor);
        Vector3[] centeredVertices = OffsetVertices(vertices, stageCenterX);
        mesh.vertices = centeredVertices;
        mesh.RecalculateBounds();

        // MeshFilterとMeshRendererを追加
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        // メッシュをMeshFilterに設定
        meshFilter.mesh = mesh;

        // カスタムマテリアルを設定
        if (stageMaterial != null)
        {
            meshRenderer.material = stageMaterial;
        }
        else
        {
            Debug.LogWarning("Stage material is not set.");
            meshRenderer.material = new Material(Shader.Find("Standard"));
        }

        // PolygonCollider2Dを追加
        PolygonCollider2D polygonCollider = gameObject.GetComponent<PolygonCollider2D>();
        if (polygonCollider != null)
        {
            Destroy(polygonCollider); // 古いコライダーを破棄
        }
        polygonCollider = gameObject.AddComponent<PolygonCollider2D>();

/*
        Vector2[] colliderPoints = new Vector2[centeredVertices.Length];
        for (int i = 0; i < centeredVertices.Length; i++)
        {
            colliderPoints[i] = new Vector2(centeredVertices[i].x, centeredVertices[i].y);
        }
        polygonCollider.points = colliderPoints;
*/
        
        Vector2[] colliderPoints = new Vector2[4];
        colliderPoints[0] = new Vector2(centeredVertices[2].x, centeredVertices[2].y);
        colliderPoints[1] = new Vector2(centeredVertices[centeredVertices.Length - 3].x, centeredVertices[centeredVertices.Length - 3].y);
        colliderPoints[2] = new Vector2(centeredVertices[centeredVertices.Length - 2].x, centeredVertices[centeredVertices.Length - 2].y);
        colliderPoints[3] = new Vector2(centeredVertices[1].x, centeredVertices[1].y);

        polygonCollider.points = colliderPoints;

    }

    float CalculateStageCenterX(Vector3[] vertices, float overlapFactor)
    {
        float totalWidth = 0.0f;
        for (int i = 0; i < vertices.Length; i += 3)
        {
            float leftX = vertices[i + 2].x;
            float rightX = vertices[i].x;
            float width = Mathf.Abs(rightX - leftX);
            totalWidth += width;
        }

        // 重なりの部分を引く
        float totalOverlap = (vertices.Length / 3 - 1) * maxWidth * overlapFactor;
        totalWidth -= totalOverlap;

        // ステージの中心を計算して返す
        return totalWidth / 2.0f;
    }

    Vector3[] OffsetVertices(Vector3[] vertices, float offsetX)
    {
        Vector3[] offsetVertices = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            offsetVertices[i] = new Vector3(vertices[i].x - offsetX, vertices[i].y, 0); // Z軸を0に設定
        }
        return offsetVertices;
    }
}
