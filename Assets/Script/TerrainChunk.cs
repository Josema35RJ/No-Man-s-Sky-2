using UnityEngine;

public class TerrainChunk
{
    public GameObject chunkObject;
    private Vector2 positionOffset; 
    private float size; 
    private Vector3 localUp; 
    
    private int lodLevel;
    private ProceduralPlanet planet;
    
    private TerrainChunk[] children;
    private bool isSubdivided;
    
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Vector3 centerPointOnSphere; 

    private GameObject propsContainer; 

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
        if(propsContainer != null) propsContainer.SetActive(false);
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
        if(propsContainer != null) propsContainer.SetActive(true);
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

        // Solo poblamos la flora en el nivel más detallado y cercano
        if (lodLevel == planet.maxLOD)
        {
            SpawnEcosystem(mesh.vertices, mesh.normals, mesh.uv);
        }
    }

    private void SpawnEcosystem(Vector3[] vertices, Vector3[] normals, Vector2[] uvs)
    {
        if (planet.ecosistema == null || planet.ecosistema.Length == 0) return;

        propsContainer = new GameObject("Ecosistema");
        propsContainer.transform.SetParent(chunkObject.transform);
        propsContainer.transform.localPosition = Vector3.zero;

        // Semilla matemática absoluta basada en el espacio tridimensional y la semilla del planeta
        Random.InitState((int)(centerPointOnSphere.x * 100f + centerPointOnSphere.y * 50f + centerPointOnSphere.z * 100f + planet.seedOffset.x));

        float oceanRealHeight = planet.planetRadius + (planet.oceanLevel * planet.heightMultiplier);

        for (int i = 0; i < planet.maxPropsPerChunk; i++)
        {
            int vIndex = Random.Range(0, vertices.Length);
            Vector3 point = vertices[vIndex];
            Vector3 normal = normals[vIndex];
            Vector2 biomeData = uvs[vIndex]; 

            Vector3 worldPos = planet.transform.TransformPoint(point);
            float pointAltitude = Vector3.Distance(planet.transform.position, worldPos);
            float slopeAngle = Vector3.Angle(point.normalized, normal);

            foreach (var prop in planet.ecosistema)
            {
                if (prop.prefab == null) continue;

                // 1. AGRUPACIÓN ORGÁNICA (CLUMPING) CON SEMILLA UNIVERSAL
                if (prop.useNoiseClumping)
                {
                    // Sumamos la semilla del planeta al ruido para que cada planeta tenga bosques únicos
                    float noiseX = (worldPos.x + planet.seedOffset.x) / prop.noiseScale;
                    float noiseZ = (worldPos.z + planet.seedOffset.z) / prop.noiseScale;
                    float noiseVal = Mathf.PerlinNoise(noiseX, noiseZ);
                    
                    if (noiseVal < (1f - prop.spawnProbability)) continue; 
                }
                else
                {
                    if (Random.value > prop.spawnProbability) continue; 
                }

                // 2. REGLAS GEOGRÁFICAS ESTRICTAS
                if (biomeData.x < prop.minTemperature || biomeData.x > prop.maxTemperature) continue; 
                if (biomeData.y < prop.minHumidity || biomeData.y > prop.maxHumidity) continue; 
                if (slopeAngle > prop.maxSlopeAngle) continue; 
                if (pointAltitude < oceanRealHeight + prop.minAltitude) continue; 

                // 3. INSTANCIACIÓN OPTIMIZADA
                GameObject newProp = Object.Instantiate(prop.prefab, worldPos, Quaternion.identity);
                newProp.transform.SetParent(propsContainer.transform);

                // 4. ESCALADO Y HUNDIMIENTO RAÍZ
                float scale = Random.Range(prop.minScale, prop.maxScale);
                newProp.transform.localScale = Vector3.one * scale;
                // Empujamos el objeto hacia dentro de la tierra para fundirlo con el terreno
                newProp.transform.position -= normal * (prop.sinkIntoGround * scale);

                // 5. ROTACIÓN Y ALINEACIÓN DE GRAVEDAD MEZCLADA
                Vector3 gravityUp = point.normalized; 
                Vector3 terrainUp = normal; 
                Vector3 finalUp = Vector3.Slerp(gravityUp, terrainUp, prop.alignToTerrainInfluence);

                newProp.transform.rotation = Quaternion.FromToRotation(Vector3.up, finalUp);
                // Rotación aleatoria en el eje Y local para dar variedad natural
                newProp.transform.Rotate(Vector3.up, Random.Range(0f, 360f), Space.Self);

                break; // Objeto plantado con éxito, pasamos a la siguiente semilla
            }
        }
    }
}