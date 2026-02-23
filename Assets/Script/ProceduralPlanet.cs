using UnityEngine;

public class ProceduralPlanet : MonoBehaviour
{
    public enum PlanetType { Terrestre, Helado, Desierto, Volcanico, Gaseoso }

    [Header("Sistema No Man's Sky")]
    public PlanetType planetType = PlanetType.Terrestre;
    
    public ComputeShader terrainComputeShader;
    public Material planetMaterial; 
    
    [Header("Escala del Planeta (Tamaño Real)")]
    public float planetRadius = 1000f; // ¡Planetas gigantes!
    [Range(2, 64)] public int resolution = 40; 

    [Header("Configuración de Terreno")]
    public float noiseScale = 400f; // Estira las montañas para que sean inmensas
    [Range(1, 8)] public int octaves = 5;
    [Range(0f, 1f)] public float persistence = 0.4f;
    public float lacunarity = 2f;
    public float heightMultiplier = 60f; // Altura real de montañas
    [Range(-1f, 1f)] public float oceanLevel = 0f;
    public Vector3 seedOffset;

    [Header("Materiales Extra")]
    public Material waterMaterial;
    public Material atmosphereMaterial;
    [Range(1.01f, 1.5f)] public float atmosphereScale = 1.05f;

    [Header("Físicas")]
    public float gravity = -12f;

    [Header("Configuración LOD (Quadtree)")]
    public Transform playerViewer; 
    public int maxLOD = 5; // Añadimos un nivel más de detalle por el tamaño
    // Ahora las distancias son gigantescas para abarcar el nuevo tamaño
    public float[] detailLevelDistances = new float[] { 2000f, 1000f, 400f, 100f, 30f, 10f }; 

    public bool autoUpdate = true;

    private TerrainChunk[] rootChunks;
    private GameObject waterObject;
    private GameObject atmosphereObject;
    private Mesh sharedBaseMesh; 

    void Start()
    {
        ApplyArchetypeSettings();
        GenerateBaseSphereForExtras();
        UpdateExtraSpheres();
        InitializeQuadTree();
    }

    void OnValidate()
    {
        if (autoUpdate)
        {
            ApplyArchetypeSettings();
            if (Application.isPlaying && rootChunks != null)
            {
                UpdateExtraSpheres();
                foreach(var c in rootChunks) c.UpdateChunk(playerViewer ? playerViewer.position : Vector3.zero);
            }
        }
    }

    private void ApplyArchetypeSettings()
    {
        // AHORA LOS ARQUETIPOS TIENEN ESCALAS REALISTAS
        switch (planetType)
        {
            case PlanetType.Terrestre:
                planetRadius = 1000f;
                heightMultiplier = 80f;  // Montañas altas
                oceanLevel = 0.1f; 
                noiseScale = 500f;       // Continentes muy anchos
                break;
            case PlanetType.Desierto:
                planetRadius = 1000f;
                heightMultiplier = 35f;  // Dunas más suaves
                oceanLevel = -1f;        // Sin océanos
                noiseScale = 700f;       // Llanuras desérticas interminables
                break;
            case PlanetType.Helado:
                planetRadius = 900f;
                heightMultiplier = 50f;  
                oceanLevel = -0.5f; 
                noiseScale = 350f;       // Terreno más roto y agrietado
                break;
            case PlanetType.Volcanico:
                planetRadius = 1100f;
                heightMultiplier = 120f; // Picos altísimos y escarpados
                oceanLevel = 0.05f;      // Lagos de lava (con el material adecuado)
                noiseScale = 250f;       // Muy caótico
                break;
            case PlanetType.Gaseoso:
                planetRadius = 4000f;    // Colosal
                heightMultiplier = 0f; 
                oceanLevel = 1f; 
                atmosphereScale = 1.02f;
                break;
        }
    }

    void InitializeQuadTree()
    {
        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        rootChunks = new TerrainChunk[6];
        for (int i = 0; i < 6; i++)
        {
            rootChunks[i] = new TerrainChunk(this, 0, directions[i], Vector2.zero, 1f, this.transform);
        }
    }

    void Update()
    {
        if (playerViewer != null && rootChunks != null)
        {
            foreach (TerrainChunk chunk in rootChunks) chunk.UpdateChunk(playerViewer.position);
        }
    }

    public void ProcessChunkMesh(Mesh mesh, Vector3[] vertices, Vector2[] uvs)
    {
        int vec3Size = sizeof(float) * 3;
        int vec2Size = sizeof(float) * 2;

        ComputeBuffer vertexBuffer = new ComputeBuffer(vertices.Length, vec3Size);
        ComputeBuffer uvBuffer = new ComputeBuffer(uvs.Length, vec2Size);

        vertexBuffer.SetData(vertices);
        uvBuffer.SetData(uvs);

        int kernel = terrainComputeShader.FindKernel("GenerateTerrain");

        terrainComputeShader.SetBuffer(kernel, "vertices", vertexBuffer);
        terrainComputeShader.SetBuffer(kernel, "uvs", uvBuffer);
        
        terrainComputeShader.SetInt("vertexCount", vertices.Length);
        terrainComputeShader.SetInt("planetType", (int)planetType);
        terrainComputeShader.SetFloat("planetRadius", planetRadius);
        terrainComputeShader.SetFloat("noiseScale", noiseScale);
        terrainComputeShader.SetInt("octaves", octaves);
        terrainComputeShader.SetFloat("persistence", persistence);
        terrainComputeShader.SetFloat("lacunarity", lacunarity);
        terrainComputeShader.SetFloat("heightMultiplier", heightMultiplier);
        terrainComputeShader.SetFloat("oceanLevel", oceanLevel);
        terrainComputeShader.SetFloats("offset", new float[] { seedOffset.x, seedOffset.y, seedOffset.z });

        int threadGroups = Mathf.CeilToInt(vertices.Length / 64f);
        terrainComputeShader.Dispatch(kernel, threadGroups, 1, 1);

        vertexBuffer.GetData(vertices);
        uvBuffer.GetData(uvs);

        vertexBuffer.Release();
        uvBuffer.Release();

        mesh.vertices = vertices;
        mesh.uv = uvs;
    }

    public void EditTerrain(Vector3 point, bool addHeight, float radius = 2f, float strength = 0.5f)
    {
        Debug.Log("Has hecho clic para editar.");
    }

    public void Attract(Transform body)
    {
        Vector3 gravityUp = (body.position - transform.position).normalized;
        Vector3 bodyUp = body.up;

        Rigidbody rb = body.GetComponent<Rigidbody>();
        if (rb != null) rb.AddForce(gravityUp * gravity);

        Quaternion targetRotation = Quaternion.FromToRotation(bodyUp, gravityUp) * body.rotation;
        body.rotation = Quaternion.Slerp(body.rotation, targetRotation, 50f * Time.deltaTime);
    }

    private void UpdateExtraSpheres()
    {
        float waterRadius = planetRadius + (oceanLevel * heightMultiplier);
        float atmoRadius = planetRadius * atmosphereScale;

        if (waterObject == null && waterMaterial != null) waterObject = CreateChildSphere("Ocean", waterMaterial);
        if (waterObject != null)
        {
            float scaleW = waterRadius / planetRadius;
            waterObject.transform.localScale = new Vector3(scaleW, scaleW, scaleW);
            waterObject.SetActive(planetType != PlanetType.Desierto && planetType != PlanetType.Gaseoso); 
        }

        if (atmosphereObject == null && atmosphereMaterial != null) atmosphereObject = CreateChildSphere("Atmosphere", atmosphereMaterial);
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
        
        mf.sharedMesh = sharedBaseMesh; 
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = objName == "Ocean"; 

        return obj;
    }

    private void GenerateBaseSphereForExtras()
    {
        sharedBaseMesh = new Mesh(); 
        sharedBaseMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        int res = 40; 
        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        int numVertices = res * res * 6;
        int numTriangles = (res - 1) * (res - 1) * 6 * 6;
        Vector3[] v = new Vector3[numVertices];
        int[] t = new int[numTriangles];

        int vIndex = 0;
        int tIndex = 0;

        foreach (Vector3 dir in directions)
        {
            Vector3 axisA = new Vector3(dir.y, dir.z, dir.x);
            Vector3 axisB = Vector3.Cross(dir, axisA);
            
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    Vector2 percent = new Vector2(x, y) / (res - 1f);
                    Vector3 pointOnUnitCube = dir + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
                    v[vIndex] = pointOnUnitCube.normalized * planetRadius;

                    if (x != res - 1 && y != res - 1)
                    {
                        t[tIndex] = vIndex; t[tIndex + 1] = vIndex + res + 1; t[tIndex + 2] = vIndex + res;
                        t[tIndex + 3] = vIndex; t[tIndex + 4] = vIndex + 1; t[tIndex + 5] = vIndex + res + 1;
                        tIndex += 6;
                    }
                    vIndex++;
                }
            }
        }
        sharedBaseMesh.vertices = v;
        sharedBaseMesh.triangles = t;
        sharedBaseMesh.RecalculateNormals();
    }
}