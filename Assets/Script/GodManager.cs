using UnityEngine;
using System.Collections.Generic;

public class GodManager : MonoBehaviour
{
    public ProceduralPlanet planetPrefab;
    public GameObject playerInstance;
    public int numberOfPlanets = 5;
    public float universeSize = 80f;
    public float minDistanceBetweenPlanets = 30f; // Distancia mínima de seguridad

    void Start()
    {
        CreateUniverse();
    }

    void update()
    {
        FindClosestPlanet();
    }

    void FindClosestPlanet()
    {
        float closestDist = Mathf.Infinity;
        ProceduralPlanet closestPlanet = null;

        // Podrías guardar la lista de planetas creados en CreateUniverse()
        foreach (Transform child in transform)
        {
            float dist = Vector3.Distance(playerInstance.transform.position, child.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestPlanet = child.GetComponent<ProceduralPlanet>();
            }
        }

        if (closestPlanet != null)
            playerInstance.GetComponent<SphericalGravity>().planet = closestPlanet.transform;
    }

    void CreateUniverse()
    {
        List<Vector3> planetPositions = new List<Vector3>();

        for (int i = 0; i < numberOfPlanets; i++)
        {
            Vector3 randomPosition = Vector3.zero;
            bool validPosition = false;
            int attempts = 0;

            // Bucle para encontrar una posición vacía (evita que aparezcan uno dentro del otro)
            while (!validPosition && attempts < 100)
            {
                randomPosition = Random.insideUnitSphere * universeSize;
                validPosition = true;

                foreach (Vector3 pos in planetPositions)
                {
                    if (Vector3.Distance(randomPosition, pos) < minDistanceBetweenPlanets)
                    {
                        validPosition = false; // Están muy cerca, intenta otra vez
                        break;
                    }
                }
                attempts++;
            }

            planetPositions.Add(randomPosition);

            // Instanciamos el planeta y lo hacemos hijo del GodManager (this.transform)
            ProceduralPlanet newPlanet = Instantiate(planetPrefab, randomPosition, Quaternion.identity, this.transform);
            
            newPlanet.seedOffset = new Vector3(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f));
            newPlanet.planetRadius = Random.Range(8f, 15f);
            
            float tipoPlaneta = Random.value; 

            if (tipoPlaneta < 0.3f) 
            {
                newPlanet.oceanLevel = Random.Range(0.2f, 0.6f); 
                newPlanet.heightMultiplier = Random.Range(1f, 3f);
                newPlanet.name = "Oceanico_" + i;
            }
            else if (tipoPlaneta < 0.6f) 
            {
                newPlanet.oceanLevel = Random.Range(-0.8f, -0.5f);
                newPlanet.heightMultiplier = Random.Range(0.5f, 1.5f);
                newPlanet.noiseScale = Random.Range(8f, 12f);
                newPlanet.name = "Arido_" + i;
            }
            else 
            {
                newPlanet.oceanLevel = Random.Range(-0.1f, 0.1f);
                newPlanet.heightMultiplier = Random.Range(2f, 4f);
                newPlanet.name = "Terrestre_" + i;
            }
        }
    }
}