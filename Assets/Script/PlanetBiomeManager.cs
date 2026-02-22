using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Esta clase define cómo es un Bioma individual
[System.Serializable]
public class BiomeConfig
{
    public string biomeName;
    [Range(0f, 1f)] 
    [Tooltip("El valor de ruido mínimo para que aparezca este bioma (0 a 1)")]
    public float minNoiseValue; 
    
    public Color biomeColor;
    public GameObject lootPrefab;
    public GameObject[] treePrefabs;
    
    [Range(0f, 1f)]
    public float treeSpawnChance = 0.5f; // Algunos biomas (como el desierto) tendrán menos árboles
}

public class PlanetBiomeManager : MonoBehaviour
{
    [Header("Configuración Global")]
    [SerializeField] private int planetResolution = 256;
    [SerializeField] private float noiseScale = 3f; // Zonas más grandes o más pequeñas
    [SerializeField] private float planetRadius = 30f; // Asegúrate de que coincida con tu ProceduralPlanet
    [SerializeField] private LayerMask planetLayer;

    [Header("Mis Biomas")]
    // ¡Ahora puedes tener tantos biomas como quieras!
    [SerializeField] private BiomeConfig[] biomes;

    [Header("Generación de Objetos")]
    [SerializeField] private GameObject[] rockPrefabs;
    [SerializeField] private int totalObjectsToSpawn = 150; // Árboles y rocas totales

    private Texture2D biomeMap;
    private ProceduralPlanet myGravity;

    IEnumerator Start()
    {
        myGravity = GetComponent<ProceduralPlanet>();

        // Esperamos a que el Compute Shader deforme la malla del planeta
        yield return null; 

        // Ordenamos los biomas por su valor mínimo para evitar errores de cálculo
        System.Array.Sort(biomes, (a, b) => a.minNoiseValue.CompareTo(b.minNoiseValue));

        GeneratePlanetTexture();
        Physics.SyncTransforms();

        GenerateWorldObjects();
    }

    void GeneratePlanetTexture()
    {
        Renderer ren = GetComponent<Renderer>();
        biomeMap = new Texture2D(planetResolution, planetResolution);
        biomeMap.filterMode = FilterMode.Point;
        biomeMap.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < planetResolution; y++)
        {
            for (int x = 0; x < planetResolution; x++)
            {
                float xCoord = (float)x / planetResolution * noiseScale;
                float yCoord = (float)y / planetResolution * noiseScale;
                
                // Ruido entre 0.0 y 1.0
                float noiseVal = Mathf.PerlinNoise(xCoord, yCoord);

                BiomeConfig currentBiome = GetBiomeFromNoise(noiseVal);
                biomeMap.SetPixel(x, y, currentBiome != null ? currentBiome.biomeColor : Color.white);
            }
        }
        biomeMap.Apply();
        
        if(ren.material.HasProperty("_MainTex")) 
        {
            ren.material.mainTexture = biomeMap;
        }
    }

    // Encuentra qué bioma corresponde a un valor de ruido específico
    BiomeConfig GetBiomeFromNoise(float noiseValue)
    {
        BiomeConfig selectedBiome = biomes[0];
        foreach (var biome in biomes)
        {
            if (noiseValue >= biome.minNoiseValue)
            {
                selectedBiome = biome;
            }
        }
        return selectedBiome;
    }

    void GenerateWorldObjects()
    {
        if (biomes.Length == 0) return;

        int spawned = 0;
        int maxAttempts = totalObjectsToSpawn * 10;
        int attempts = 0;

        float raycastStartHeight = planetRadius + 100f; // Muy alto para no fallar con las montañas

        while (spawned < totalObjectsToSpawn && attempts < maxAttempts)
        {
            attempts++;
            Vector3 randomDir = Random.onUnitSphere;
            Vector3 rayOrigin = transform.position + (randomDir * raycastStartHeight);
            Vector3 rayDirection = (transform.position - rayOrigin).normalized;

            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, raycastStartHeight + 20f, planetLayer))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    // Calculamos de nuevo el ruido para esa posición exacta
                    Vector2 uv = hit.textureCoord;
                    float noiseVal = Mathf.PerlinNoise(uv.x * noiseScale, uv.y * noiseScale);
                    
                    BiomeConfig targetBiome = GetBiomeFromNoise(noiseVal);

                    // Decidimos si plantamos un árbol o una roca interactiva
                    bool spawnTree = Random.value < targetBiome.treeSpawnChance;

                    if (spawnTree && targetBiome.treePrefabs.Length > 0)
                    {
                        GameObject treePrefab = targetBiome.treePrefabs[Random.Range(0, targetBiome.treePrefabs.Length)];
                        SpawnObject(treePrefab, hit.point, targetBiome, false);
                    }
                    else if (rockPrefabs.Length > 0)
                    {
                        GameObject rockPrefab = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
                        SpawnObject(rockPrefab, hit.point, targetBiome, true);
                    }
                    
                    spawned++;
                }
            }
        }
    }

    void SpawnObject(GameObject prefab, Vector3 position, BiomeConfig biome, bool isRock)
    {
        GameObject newObj = Instantiate(prefab, position, Quaternion.identity);
        
        // Alineación esférica
        newObj.transform.up = (position - transform.position).normalized;
        newObj.transform.parent = transform;

        // Físicas de gravedad
        GravityBody gb = newObj.GetComponent<GravityBody>();
        if (gb == null) gb = newObj.AddComponent<GravityBody>();
        gb.planet = myGravity;

        if (isRock)
        {
            RockInteraction rockScript = newObj.GetComponent<RockInteraction>();
            if (rockScript == null) rockScript = newObj.AddComponent<RockInteraction>();
            rockScript.LootToSpawn = biome.lootPrefab; // ¡El loot depende del bioma!
        }
        else
        {
            // Variación de tamaño para los árboles
            newObj.transform.localScale *= Random.Range(0.7f, 1.3f);
        }
    }
}