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
    
    void Start()
    {
        // アンチエイリアシングを4xに設定
        QualitySettings.antiAliasing = 4;
        GenerateStage();
    }

    public void GenerateStage()
    {
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
            vertices[i * 3] = new Vector3(currentX, baseY, 0); // 左下
            vertices[i * 3 + 1] = new Vector3(currentX + width, baseY, 0); // 右下
            vertices[i * 3 + 2] = new Vector3(currentX + width / 2, baseY - height, 0); // 上 -> 反転して下に変更

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

        // ステージの中心を計算してオフセット
        float stageCenterX = CalculateStageCenterX(vertices, overlapFactor);
        Vector3[] centeredVertices = OffsetVertices(vertices, stageCenterX);
        mesh.vertices = centeredVertices;
        mesh.RecalculateBounds();

        // MeshFilterとMeshRendererを追加
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

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
        PolygonCollider2D polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        Vector2[] colliderPoints = new Vector2[centeredVertices.Length];

        for (int i = 0; i < centeredVertices.Length; i++)
        {
            colliderPoints[i] = new Vector2(centeredVertices[i].x, centeredVertices[i].y);
        }
        polygonCollider.points = colliderPoints;
    }

    float CalculateStageCenterX(Vector3[] vertices, float overlapFactor)
    {
        float totalWidth = 0.0f;
        for (int i = 0; i < vertices.Length; i += 3)
        {
            float leftX = vertices[i].x;
            float rightX = vertices[i + 1].x;
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
