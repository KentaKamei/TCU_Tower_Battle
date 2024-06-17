using UnityEngine;

public class StageGenerator : MonoBehaviour
{
    public int minTriangles = 5;
    public int maxTriangles = 10;
    public float maxWidth = 1.0f;
    public float minHeight = 0.5f;
    public float maxHeight = 1.5f;
    public float baseY = 0.0f; // 三角形の底辺のy座標
    public float overlapFactor = 0.2f; // 重なりの度合い
    public Material stageMaterial; // ステージ用のマテリアル

    void Start()
    {
        // アンチエイリアシングを4xに設定
        QualitySettings.antiAliasing = 4;
        GenerateStage();
    }

    void GenerateStage()
    {
        Mesh mesh = new Mesh();

        int triangleCount = Random.Range(minTriangles, maxTriangles);
        Vector3[] vertices = new Vector3[triangleCount * 3];
        int[] triangles = new int[triangleCount * 3];

        // 初期X座標を計算して調整
        float currentX = -1.62f;

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
            offsetVertices[i] = new Vector3(vertices[i].x - offsetX, vertices[i].y, vertices[i].z);
        }
        return offsetVertices;
    }
}
