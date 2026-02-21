using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralPlanet : MonoBehaviour
{
    public ComputeShader terrainComputeShader;
    
    [Header("Escala del Planeta")]
    public float planetRadius = 10f;
    [Range(2, 256)] public int resolution = 100;

    [Header("Configuración de Terreno")]
    public float noiseScale = 5f;
    [Range(1, 8)] public int octaves = 5;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;
    public float heightMultiplier = 2f;
    [Range(-1f, 1f)] public float oceanLevel = 0f;
    public Vector3 seedOffset;

    [Header("Materiales Extra (Agua y Atmósfera)")]
    public Material waterMaterial;
    public Material atmosphereMaterial;
    [Range(1.05f, 1.5f)] public float atmosphereScale = 1.2f;

    public bool autoUpdate = true;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] displacedVertices;
    private Vector2[] uvs;

    // Referencias a los objetos hijos
    private GameObject waterObject;
    private GameObject atmosphereObject;
    private Mesh sharedBaseMesh; // Malla perfecta que usarán el agua y la atmósfera

    void Start()
    {
        GenerateQuadSphere();
        GenerateTerrainGPU();
        UpdateExtraSpheres();
    }

    void OnValidate()
    {
        if (autoUpdate && Application.isPlaying && baseVertices != null)
        {
            GenerateTerrainGPU();
            UpdateExtraSpheres();
        }
    }

    public void GenerateTerrainGPU()
    {
        if (baseVertices == null || baseVertices.Length == 0) return;

        System.Array.Copy(baseVertices, displacedVertices, baseVertices.Length);

        int vec3Size = sizeof(float) * 3;
        int vec2Size = sizeof(float) * 2;

        ComputeBuffer vertexBuffer = new ComputeBuffer(displacedVertices.Length, vec3Size);
        ComputeBuffer uvBuffer = new ComputeBuffer(uvs.Length, vec2Size);

        vertexBuffer.SetData(displacedVertices);
        uvBuffer.SetData(uvs);

        int kernel = terrainComputeShader.FindKernel("GenerateTerrain");

        terrainComputeShader.SetBuffer(kernel, "vertices", vertexBuffer);
        terrainComputeShader.SetBuffer(kernel, "uvs", uvBuffer);
        
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
        uvBuffer.GetData(uvs);

        vertexBuffer.Release();
        uvBuffer.Release();

        mesh.vertices = displacedVertices;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    // Genera y actualiza el tamaño del Agua y la Atmósfera
    private void UpdateExtraSpheres()
    {
        float waterRadius = planetRadius + (oceanLevel * heightMultiplier);
        float atmoRadius = planetRadius * atmosphereScale;

        // Océano
        if (waterObject == null && waterMaterial != null) 
            waterObject = CreateChildSphere("Ocean", waterMaterial);
        
        if (waterObject != null)
        {
            // Escalamos la esfera perfecta para que coincida exactamente con el nivel del mar
            float scaleW = waterRadius / planetRadius;
            waterObject.transform.localScale = new Vector3(scaleW, scaleW, scaleW);
        }

        // Atmósfera
        if (atmosphereObject == null && atmosphereMaterial != null) 
            atmosphereObject = CreateChildSphere("Atmosphere", atmosphereMaterial);
        
        if (atmosphereObject != null)
        {
            float scaleA = atmoRadius / planetRadius;
            atmosphereObject.transform.localScale = new Vector3(scaleA, scaleA, scaleA);
        }
    }

    private GameObject CreateChildSphere(string objName, Material mat)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(this.transform);
        obj.transform.localPosition = Vector3.zero;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        
        mf.sharedMesh = sharedBaseMesh; // Usamos la esfera sin deformar
        mr.sharedMaterial = mat;

        // Quitar colisiones y sombras a la atmósfera/agua
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = objName == "Ocean"; // El agua sí puede recibir sombras

        return obj;
    }

    private void GenerateQuadSphere()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        sharedBaseMesh = new Mesh(); // Guardamos una copia pura
        
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        sharedBaseMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        
        int numVertices = resolution * resolution * 6;
        int numTriangles = (resolution - 1) * (resolution - 1) * 6 * 6;

        baseVertices = new Vector3[numVertices];
        displacedVertices = new Vector3[numVertices];
        uvs = new Vector2[numVertices];
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
        
        sharedBaseMesh.vertices = baseVertices;
        sharedBaseMesh.triangles = triangles;
        sharedBaseMesh.RecalculateNormals();
    }
}