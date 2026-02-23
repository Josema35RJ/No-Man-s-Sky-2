using UnityEngine;
using System.Collections.Generic;

public class GodManager : MonoBehaviour
{
    public ProceduralPlanet planetPrefab;
    public GameObject playerInstance;
    public int numberOfPlanets = 5;
    
    [Header("Escalas Cósmicas")]
    // Hacemos el universo inmenso y separamos los planetas muchísimo
    public float universeSize = 100000f; 
    public float minDistanceBetweenPlanets = 20000f; 

    void Start()
    {
        CreateUniverse();
    }

    void Update()
    {
        FindClosestPlanet();
    }

    void FindClosestPlanet()
    {
        float closestDist = Mathf.Infinity;
        ProceduralPlanet closestPlanet = null;

        foreach (Transform child in transform)
        {
            float dist = Vector3.Distance(playerInstance.transform.position, child.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestPlanet = child.GetComponent<ProceduralPlanet>();
            }
        }

        if (closestPlanet != null && playerInstance.GetComponent<SphericalGravity>() != null)
        {
            playerInstance.GetComponent<SphericalGravity>().planet = closestPlanet.transform;
        }
    }

    void CreateUniverse()
    {
        List<Vector3> planetPositions = new List<Vector3>();

        for (int i = 0; i < numberOfPlanets; i++)
        {
            Vector3 randomPosition = Vector3.zero;
            bool validPosition = false;
            int attempts = 0;

            // Bucle para encontrar una posición vacía en el inmenso espacio
            while (!validPosition && attempts < 100)
            {
                randomPosition = Random.insideUnitSphere * universeSize;
                validPosition = true;

                foreach (Vector3 pos in planetPositions)
                {
                    if (Vector3.Distance(randomPosition, pos) < minDistanceBetweenPlanets)
                    {
                        validPosition = false; 
                        break;
                    }
                }
                attempts++;
            }

            planetPositions.Add(randomPosition);

            // Instanciamos el planeta
            ProceduralPlanet newPlanet = Instantiate(planetPrefab, randomPosition, Quaternion.identity, this.transform);
            
            // 1. FUNDAMENTAL: Decirle al planeta quién es el jugador para que funcione el Quadtree (LOD)
            newPlanet.playerViewer = playerInstance.transform;

            // 2. Semilla única para que el terreno (montañas y continentes) sea 100% distinto
            newPlanet.seedOffset = new Vector3(Random.Range(-5000f, 5000f), Random.Range(-5000f, 5000f), Random.Range(-5000f, 5000f));
            
            // 3. Elegimos el Arquetipo base (Terrestre, Helado, Volcanico, etc.)
            newPlanet.planetType = (ProceduralPlanet.PlanetType)Random.Range(0, 5);
            newPlanet.name = newPlanet.planetType.ToString() + "_" + i;
            
            // 4. (Opcional) Pequeña variación de radio extra sobre el arquetipo
            newPlanet.planetRadius *= Random.Range(0.8f, 1.2f);
        }
    }
}