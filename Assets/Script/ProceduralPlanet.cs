using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ProceduralPlanet : MonoBehaviour
{
    public ComputeShader terrainComputeShader;
    
    [Header("Escala del Planeta")]
    public float planetRadius = 10f;
    [Range(2, 256)] public int resolution = 150;

    [Header("Configuración de Terreno")]
    public float noiseScale = 5f;
    [Range(1, 8)] public int octaves = 5;
    [Range(0f, 1f)] public float persistence = 0.4f;
    public float lacunarity = 2f;
    public float heightMultiplier = 2f;
    [Range(-1f, 1f)] public float oceanLevel = 0f;
    public Vector3 seedOffset;

    [Header("Materiales Extra (Agua y Atmósfera)")]
    public Material waterMaterial;
    public Material atmosphereMaterial;
    [Range(1.05f, 1.5f)] public float atmosphereScale = 1.2f;

    [Header("Gravedad")]
    public float gravity = -12f; // <--- AQUÍ ESTÁ LA VARIABLE QUE FALTABA

    public bool autoUpdate = true;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] displacedVertices;
    private Vector2[] uvs;

    private GameObject waterObject;
    private GameObject atmosphereObject;
    private Mesh sharedBaseMesh; 
    
    private MeshCollider meshCollider;

    void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        GenerateQuadSphere();
        GenerateTerrainGPU();
        UpdateExtraSpheres();
        
        meshCollider.sharedMesh = mesh;
    }

    void OnValidate()
    {
        if (autoUpdate && Application.isPlaying && baseVertices != null)
        {
            GenerateTerrainGPU();
            UpdateExtraSpheres();
            if (meshCollider != null) meshCollider.sharedMesh = mesh;
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

    public void EditTerrain(Vector3 point, bool addHeight, float radius = 2f, float strength = 0.5f)
    {
        int kernel = terrainComputeShader.FindKernel("EditTerrain");
        
        int vec3Size = sizeof(float) * 3;
        ComputeBuffer vertexBuffer = new ComputeBuffer(displacedVertices.Length, vec3Size);
        vertexBuffer.SetData(displacedVertices);
        
        terrainComputeShader.SetBuffer(kernel, "vertices", vertexBuffer);
        
        Vector3 localPoint = transform.InverseTransformPoint(point);
        terrainComputeShader.SetFloats("brushPosition", new float[] { localPoint.x, localPoint.y, localPoint.z });
        
        terrainComputeShader.SetFloat("brushRadius", radius);
        terrainComputeShader.SetFloat("brushStrength", addHeight ? strength : -strength);
        terrainComputeShader.SetInt("vertexCount", displacedVertices.Length);

        int threadGroups = Mathf.CeilToInt(displacedVertices.Length / 64f);
        terrainComputeShader.Dispatch(kernel, threadGroups, 1, 1);

        vertexBuffer.GetData(displacedVertices);
        vertexBuffer.Release();

        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

    private void UpdateExtraSpheres()
    {
        float waterRadius = planetRadius + (oceanLevel * heightMultiplier);
        float atmoRadius = planetRadius * atmosphereScale;

        if (waterObject == null && waterMaterial != null) 
            waterObject = CreateChildSphere("Ocean", waterMaterial);
        
        if (waterObject != null)
        {
            float scaleW = waterRadius / planetRadius;
            waterObject.transform.localScale = new Vector3(scaleW, scaleW, scaleW);
        }

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
        
        mf.sharedMesh = sharedBaseMesh; 
        mr.sharedMaterial = mat;

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = objName == "Ocean"; 

        return obj;
    }

    private void GenerateQuadSphere()
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        sharedBaseMesh = new Mesh(); 
        
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

    // --- FUNCIÓN DE GRAVEDAD ---
    public void Attract(Transform body)
    {
        Vector3 gravityUp = (body.position - transform.position).normalized;
        Vector3 bodyUp = body.up;

        Rigidbody rb = body.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(gravityUp * gravity); // Ahora sí reconoce "gravity"
        }

        Quaternion targetRotation = Quaternion.FromToRotation(bodyUp, gravityUp) * body.rotation;
        body.rotation = Quaternion.Slerp(body.rotation, targetRotation, 50f * Time.deltaTime);
    }
}