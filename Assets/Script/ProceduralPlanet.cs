using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralPlanet : MonoBehaviour
{
    public ComputeShader terrainComputeShader;
    
    [Header("Escala del Planeta")]
    public float planetRadius = 10f;
    [Range(2, 256)] public int resolution = 100;

    [Header("Configuración de Terreno (Dios)")]
    public float noiseScale = 5f;
    [Range(1, 8)] public int octaves = 5;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float heightMultiplier = 2f;
    [Range(-1f, 1f)] public float oceanLevel = 0f;
    public Vector3 seedOffset;

    public bool autoUpdate = true;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] displacedVertices;

    void Start()
    {
        GenerateQuadSphere();
        GenerateTerrainGPU();
    }

    void OnValidate()
    {
        if (autoUpdate && Application.isPlaying && baseVertices != null)
        {
            GenerateTerrainGPU();
        }
    }

    public void GenerateTerrainGPU()
    {
        if (baseVertices == null || baseVertices.Length == 0) return;

        System.Array.Copy(baseVertices, displacedVertices, baseVertices.Length);

        int vec3Size = sizeof(float) * 3;
        ComputeBuffer vertexBuffer = new ComputeBuffer(displacedVertices.Length, vec3Size);
        vertexBuffer.SetData(displacedVertices);

        int kernel = terrainComputeShader.FindKernel("GenerateTerrain");

        terrainComputeShader.SetBuffer(kernel, "vertices", vertexBuffer);
        terrainComputeShader.SetInt("vertexCount", displacedVertices.Length);
        terrainComputeShader.SetFloat("planetRadius", planetRadius);
        terrainComputeShader.SetFloat("noiseScale", noiseScale);
        terrainComputeShader.SetInt("octaves", octaves);
        terrainComputeShader.SetFloat("persistence", persistence);
        terrainComputeShader.SetFloat("lacunarity", lacunarity);
        terrainComputeShader.SetFloat("heightMultiplier", heightMultiplier);
        terrainComputeShader.SetFloat("oceanLevel", oceanLevel);
        terrainComputeShader.SetFloats("offset", new float[] { seedOffset.x, seedOffset.y, seedOffset.z });

        int threadGroups = Mathf.CeilToInt(displacedVertices.Length / 64f);
        terrainComputeShader.Dispatch(kernel, threadGroups, 1, 1);

        vertexBuffer.GetData(displacedVertices);
        vertexBuffer.Release();

        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void GenerateQuadSphere()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        
        int numVertices = resolution * resolution * 6;
        int numTriangles = (resolution - 1) * (resolution - 1) * 6 * 6;

        baseVertices = new Vector3[numVertices];
        displacedVertices = new Vector3[numVertices];
        int[] triangles = new int[numTriangles];

        int vIndex = 0;
        int tIndex = 0;

        foreach (Vector3 dir in directions)
        {
            Vector3 axisA = new Vector3(dir.y, dir.z, dir.x);
            Vector3 axisB = Vector3.Cross(dir, axisA);
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 percent = new Vector2(x, y) / (resolution - 1);
                    Vector3 pointOnUnitCube = dir + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
                    
                    baseVertices[vIndex] = pointOnUnitCube.normalized * planetRadius;

                    if (x != resolution - 1 && y != resolution - 1)
                    {
                        triangles[tIndex] = vIndex;
                        triangles[tIndex + 1] = vIndex + resolution + 1;
                        triangles[tIndex + 2] = vIndex + resolution;
                        triangles[tIndex + 3] = vIndex;
                        triangles[tIndex + 4] = vIndex + 1;
                        triangles[tIndex + 5] = vIndex + resolution + 1;
                        tIndex += 6;
                    }
                    vIndex++;
                }
            }
        }

        mesh.vertices = baseVertices;
        mesh.triangles = triangles;
    }
}