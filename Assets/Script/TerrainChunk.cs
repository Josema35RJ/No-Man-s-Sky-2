using UnityEngine;

public class TerrainChunk
{
    public GameObject chunkObject;
    private Vector2 positionOffset; 
    private float size; 
    private Vector3 localUp; 
    private int lodLevel;
    private ProceduralPlanet planet; // Asegúrate de que aquí diga ProceduralPlanet
    private TerrainChunk[] children;
    private bool isSubdivided;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Vector3 centerPointOnSphere; 

    public TerrainChunk(ProceduralPlanet planet, int lodLevel, Vector3 localUp, Vector2 positionOffset, float size, Transform parent)
    {
        this.planet = planet;
        this.lodLevel = lodLevel;
        this.localUp = localUp;
        this.positionOffset = positionOffset;
        this.size = size;

        Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        Vector3 axisB = Vector3.Cross(localUp, axisA);
        Vector2 centerUV = positionOffset + new Vector2(size / 2f, size / 2f);
        Vector3 pointOnCube = localUp + (centerUV.x - 0.5f) * 2 * axisA + (centerUV.y - 0.5f) * 2 * axisB;
        centerPointOnSphere = planet.transform.TransformPoint(pointOnCube.normalized * planet.planetRadius);

        chunkObject = new GameObject($"Chunk_LOD_{lodLevel}");
        chunkObject.transform.SetParent(parent);
        chunkObject.transform.localPosition = Vector3.zero;

        GenerateMesh(); 
    }

    public void UpdateChunk(Vector3 viewerPosition)
    {
        float distanceToViewer = Vector3.Distance(centerPointOnSphere, viewerPosition);

        if (distanceToViewer < planet.detailLevelDistances[lodLevel] && lodLevel < planet.maxLOD)
        {
            if (!isSubdivided) Subdivide();
            foreach (var child in children) child.UpdateChunk(viewerPosition);
        }
        else
        {
            if (isSubdivided) Merge();
        }
    }

    private void Subdivide()
    {
        children = new TerrainChunk[4];
        float halfSize = size / 2f;

        children[0] = new TerrainChunk(planet, lodLevel + 1, localUp, positionOffset + new Vector2(0, halfSize), halfSize, chunkObject.transform);
        children[1] = new TerrainChunk(planet, lodLevel + 1, localUp, positionOffset + new Vector2(halfSize, halfSize), halfSize, chunkObject.transform);
        children[2] = new TerrainChunk(planet, lodLevel + 1, localUp, positionOffset + new Vector2(0, 0), halfSize, chunkObject.transform);
        children[3] = new TerrainChunk(planet, lodLevel + 1, localUp, positionOffset + new Vector2(halfSize, 0), halfSize, chunkObject.transform);

        isSubdivided = true;
        meshRenderer.enabled = false; 
        if(meshCollider) meshCollider.enabled = false; 
    }

    private void Merge()
    {
        if (children != null)
        {
            for (int i = 0; i < 4; i++)
            {
                if(children[i].chunkObject != null) Object.Destroy(children[i].chunkObject);
            }
        }
        children = null;
        isSubdivided = false;
        meshRenderer.enabled = true; 
        if(meshCollider) meshCollider.enabled = true; 
    }

    private void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        int res = planet.resolution;
        Vector3[] vertices = new Vector3[res * res];
        int[] triangles = new int[(res - 1) * (res - 1) * 6];
        Vector2[] uvs = new Vector2[res * res]; 

        Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        Vector3 axisB = Vector3.Cross(localUp, axisA);

        int vIndex = 0;
        int tIndex = 0;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                Vector2 percent = new Vector2(x, y) / (res - 1f);
                Vector2 chunkPercent = positionOffset + (percent * size); 
                Vector3 pointOnUnitCube = localUp + (chunkPercent.x - 0.5f) * 2 * axisA + (chunkPercent.y - 0.5f) * 2 * axisB;
                vertices[vIndex] = pointOnUnitCube.normalized * planet.planetRadius; 

                if (x != res - 1 && y != res - 1)
                {
                    triangles[tIndex] = vIndex;
                    triangles[tIndex + 1] = vIndex + res + 1;
                    triangles[tIndex + 2] = vIndex + res;
                    triangles[tIndex + 3] = vIndex;
                    triangles[tIndex + 4] = vIndex + 1;
                    triangles[tIndex + 5] = vIndex + res + 1;
                    tIndex += 6;
                }
                vIndex++;
            }
        }

        planet.ProcessChunkMesh(mesh, vertices, uvs);
        
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter mf = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        meshRenderer.sharedMaterial = planet.planetMaterial; 
        
        meshCollider = chunkObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }
}